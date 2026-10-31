using ECommerce.Application;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.Entities.Products.Events;
using ECommerce.Domain.Entities.ProductsBrand;
using ECommerce.Domain.Entities.ProductsType;
using ECommerce.Domain.ValueObjects;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Persistence.DbContexts;
using ECommerce.Infrastructure.Persistence.Outbox;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace ECommerce.IntegrationTests;

/// <summary>
/// Creates an isolated database and service provider for outbox/inbox tests.
/// </summary>
internal sealed class OutboxTestHost : IAsyncDisposable
{
    private const string ServerConnectionString =
        "Server=.;Trusted_Connection=True;TrustServerCertificate=True;";

    private readonly string _databaseName;

    private OutboxTestHost(ServiceProvider provider, string databaseName)
    {
        Provider = provider;
        _databaseName = databaseName;
    }

    public ServiceProvider Provider { get; }

    public static async Task<OutboxTestHost> CreateAsync(
        Action<IServiceCollection>? configureServices = null,
        Dictionary<string, string?>? settings = null,
        bool migrate = true)
    {
        var databaseName = $"ECommerce_Inbox_{Guid.NewGuid():N}";

        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] =
                $"Server=.;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;",
            ["Outbox:PollIntervalSeconds"] = "1",
            ["Outbox:BatchSize"] = "10",
            ["Outbox:MaxAttempts"] = "3",
            ["Inbox:RetentionDays"] = "30",
            ["Inbox:CleanupIntervalMinutes"] = "60",
            ["Jwt:Issuer"] = "ECommerce.Test",
            ["Jwt:Audience"] = "ECommerce.Test",
            ["Jwt:SecretKey"] =
                "test-secret-key-that-is-at-least-32-characters-long-0123456789",
        };

        if (settings is not null)
        {
            foreach (var (key, value) in settings)
            {
                values[key] = value;
            }
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(configuration);
        configureServices?.Invoke(services);

        var provider = services.BuildServiceProvider();
        var host = new OutboxTestHost(provider, databaseName);

        if (migrate)
        {
            await using var scope = provider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        return host;
    }

    public async Task<Guid> CreateProductWithOutboxMessageAsync()
    {
        await using var scope = Provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var brandResult = ProductBrand.Create(
            new ProductBrandId(Guid.NewGuid()),
            $"Test Brand {Guid.NewGuid():N}");
        var typeResult = ProductType.Create(
            new ProductTypeId(Guid.NewGuid()),
            $"Test Type {Guid.NewGuid():N}");
        var priceResult = Money.Create(19.99m, Currency.Usd);

        Assert.True(brandResult.IsSuccess);
        Assert.True(typeResult.IsSuccess);
        Assert.True(priceResult.IsSuccess);

        var productResult = Product.Create(
            new ProductId(Guid.NewGuid()),
            "Test Product",
            "Product created by an integration test",
            "https://cdn.example.com/product.png",
            priceResult.Value);

        Assert.True(productResult.IsSuccess);

        var product = productResult.Value;
        Assert.True(product.ChangeBrand(brandResult.Value.Id).IsSuccess);
        Assert.True(product.ChangeType(typeResult.Value.Id).IsSuccess);

        dbContext.BrandsSet.Add(brandResult.Value);
        dbContext.TypesSet.Add(typeResult.Value);
        dbContext.ProductsSet.Add(product);
        await dbContext.SaveChangesAsync();

        return (await dbContext.OutboxMessages.AsNoTracking()
                .SingleAsync(message => message.Type.Contains(nameof(ProductCreatedDomainEvent))))
            .Id;
    }

    public async Task RunProcessorUntilProcessedAsync(Guid messageId, TimeSpan? timeout = null)
    {
        var processor = new OutboxProcessor(
            Provider.GetRequiredService<IServiceScopeFactory>(),
            Provider.GetRequiredService<IOptions<OutboxOptions>>(),
            Provider.GetRequiredService<TimeProvider>(),
            Provider.GetRequiredService<ILogger<OutboxProcessor>>());

        using var cts = new CancellationTokenSource();
        await processor.StartAsync(cts.Token);

        try
        {
            var processed = await WaitUntilAsync(
                async () =>
                {
                    await using var scope = Provider.CreateAsyncScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    return await dbContext.OutboxMessages.AsNoTracking()
                        .AnyAsync(message =>
                            message.Id == messageId &&
                            message.Status == OutboxMessageStatus.Processed);
                },
                timeout ?? TimeSpan.FromSeconds(30));

            Assert.True(processed, $"Outbox message {messageId} was not processed in time.");
        }
        finally
        {
            cts.Cancel();
            await processor.StopAsync(CancellationToken.None);
        }
    }

    public async Task RedeliverOutboxMessageAsync(Guid messageId)
    {
        await using var scope = Provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.ExecuteSqlAsync($"""
            UPDATE OutboxMessages
            SET Status = 0, Attempts = 0, ProcessedOnUtc = NULL, Error = NULL
            WHERE Id = {messageId}
            """);
    }

    public async ValueTask DisposeAsync()
    {
        await Provider.DisposeAsync();

        await using var connection = new SqlConnection(ServerConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            IF DB_ID(N'{_databaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{_databaseName}];
            END
            """;
        await command.ExecuteNonQueryAsync();
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
