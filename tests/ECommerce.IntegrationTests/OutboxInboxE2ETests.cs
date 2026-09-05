using ECommerce.Application;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.Entities.Products.Events;
using ECommerce.Domain.Entities.ProductsBrand;
using ECommerce.Domain.Entities.ProductsType;
using ECommerce.Domain.ValueObjects;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Persistence.DbContexts;
using ECommerce.Infrastructure.Persistence.Outbox;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace ECommerce.IntegrationTests;

public sealed class OutboxInboxE2ETests : IAsyncLifetime
{
    private const string ServerConnectionString =
        "Server=.;Trusted_Connection=True;TrustServerCertificate=True;";

    private static readonly string DatabaseName = $"ECommerce_E2E_{Guid.NewGuid():N}";

    private ServiceProvider _provider = null!;

    public async Task InitializeAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    $"Server=.;Database={DatabaseName};Trusted_Connection=True;TrustServerCertificate=True;",
                ["Outbox:PollIntervalSeconds"] = "1",
                ["Outbox:BatchSize"] = "10",
                ["Outbox:MaxAttempts"] = "3",
                ["Jwt:Issuer"] = "ECommerce.Test",
                ["Jwt:Audience"] = "ECommerce.Test",
                ["Jwt:SecretKey"] =
                    "test-secret-key-that-is-at-least-32-characters-long-0123456789",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(configuration);

        services.RemoveAll<INotificationHandler<ProductCreatedDomainEvent>>();
        services.AddTransient<INotificationHandler<ProductCreatedDomainEvent>, CountingClaimHandler>();

        _provider = services.BuildServiceProvider();

        await using var scope = _provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();

        await using var connection = new SqlConnection(ServerConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            IF DB_ID(N'{DatabaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{DatabaseName}];
            END
            """;
        await command.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task DomainEvent_PublishedThroughOutbox_AndRedeliverySuppressedByInbox()
    {
        var productId = new ProductId(Guid.NewGuid());

        Guid outboxMessageId;
        string expectedEventType;

        await using (var arrangeScope = _provider.CreateAsyncScope())
        {
            var dbContext = arrangeScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var brandResult = ProductBrand.Create(new ProductBrandId(Guid.NewGuid()), "E2E Brand");
            var typeResult = ProductType.Create(new ProductTypeId(Guid.NewGuid()), "E2E Type");
            Assert.True(brandResult.IsSuccess);
            Assert.True(typeResult.IsSuccess);

            var priceResult = Money.Create(19.99m, Currency.Usd);
            Assert.True(priceResult.IsSuccess);

            var productResult = Product.Create(
                productId,
                "Mechanical Keyboard",
                "End-to-end outbox/inbox test product",
                "https://cdn.example.com/keyboard.png",
                priceResult.Value);
            Assert.True(productResult.IsSuccess);

            var product = productResult.Value;
            Assert.True(product.ChangeBrand(brandResult.Value.Id).IsSuccess);
            Assert.True(product.ChangeType(typeResult.Value.Id).IsSuccess);

            dbContext.BrandsSet.Add(brandResult.Value);
            dbContext.TypesSet.Add(typeResult.Value);
            dbContext.ProductsSet.Add(product);
            await dbContext.SaveChangesAsync();

            var outboxMessage = await dbContext.OutboxMessages.AsNoTracking()
                .SingleAsync(m => m.Type.Contains(nameof(ProductCreatedDomainEvent)));
            outboxMessageId = outboxMessage.Id;

            expectedEventType = Type.GetType(outboxMessage.Type)!.FullName!;
        }

        await using (var assertScope = _provider.CreateAsyncScope())
        {
            var dbContext = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            Assert.Equal(
                OutboxMessageStatus.Pending,
                (await dbContext.OutboxMessages.AsNoTracking().SingleAsync(m => m.Id == outboxMessageId)).Status);
            Assert.False(await dbContext.InboxMessages.AnyAsync());
        }

        await RunOutboxProcessorUntilProcessedAsync(outboxMessageId);

        Assert.Equal(1, CountingClaimHandler.AppliedCount);

        await using (var assertScope = _provider.CreateAsyncScope())
        {
            var dbContext = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var inboxMessage = await dbContext.InboxMessages.AsNoTracking()
                .SingleAsync(m => m.Id == outboxMessageId);
            Assert.Equal(expectedEventType, inboxMessage.EventType);
        }

        await RedeliverOutboxMessageAsync(outboxMessageId);
        await RunOutboxProcessorUntilProcessedAsync(outboxMessageId);

        Assert.Equal(1, CountingClaimHandler.AppliedCount);

        await using (var assertScope = _provider.CreateAsyncScope())
        {
            var dbContext = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(1, await dbContext.InboxMessages.CountAsync(m => m.Id == outboxMessageId));
        }
    }

    private async Task RunOutboxProcessorUntilProcessedAsync(Guid outboxMessageId)
    {
        await using var scope = _provider.CreateAsyncScope();

        var processor = new OutboxProcessor(
            _provider.GetRequiredService<IServiceScopeFactory>(),
            _provider.GetRequiredService<IOptions<OutboxOptions>>(),
            _provider.GetRequiredService<ILogger<OutboxProcessor>>());

        using var cts = new CancellationTokenSource();
        await processor.StartAsync(cts.Token);
        try
        {
            var processed = await WaitUntilAsync(async () =>
            {
                await using var pollScope = _provider.CreateAsyncScope();
                var dbContext = pollScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                return await dbContext.OutboxMessages.AsNoTracking()
                    .AnyAsync(m => m.Id == outboxMessageId && m.Status == OutboxMessageStatus.Processed);
            },
            TimeSpan.FromSeconds(30));

            Assert.True(processed, $"Outbox message {outboxMessageId} was not processed in time.");
        }
        finally
        {
            cts.Cancel();
            await processor.StopAsync(CancellationToken.None);
        }
    }

    private async Task RedeliverOutboxMessageAsync(Guid outboxMessageId)
    {
        await using var scope = _provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.ExecuteSqlAsync($"""
            UPDATE OutboxMessages
            SET Status = 0, Attempts = 0, ProcessedOnUtc = NULL
            WHERE Id = {outboxMessageId}
            """);
    }

    private static async Task<bool> WaitUntilAsync(Func<Task<bool>> predicate, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (await predicate())
            {
                return true;
            }

            await Task.Delay(100);
        }

        return false;
    }
}

public sealed class CountingClaimHandler(IInbox inbox) : INotificationHandler<ProductCreatedDomainEvent>
{
    public static int AppliedCount;

    public async Task Handle(ProductCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        if (!await inbox.TryClaimAsync(cancellationToken))
        {
            return;
        }

        Interlocked.Increment(ref AppliedCount);
    }
}
