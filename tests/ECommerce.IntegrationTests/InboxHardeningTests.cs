using ECommerce.Application.Common.EventHandlers;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Products.Events;
using ECommerce.Domain.Entities.ProductsBrand;
using ECommerce.Infrastructure.Persistence.DbContexts;
using ECommerce.Infrastructure.Persistence.Inbox;
using ECommerce.Infrastructure.Persistence.Outbox;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ECommerce.IntegrationTests;

public sealed class InboxHardeningTests
{
    [Fact]
    public async Task HandlerFailure_DoesNotConsumeMessage_AndRetrySucceeds()
    {
        await using var host = await OutboxTestHost.CreateAsync(
            services => RegisterConsumers(services, typeof(FirstTestConsumer)));

        host.Provider.GetRequiredService<FirstConsumerState>().FailuresBeforeSuccess = 1;

        var messageId = await host.CreateProductWithOutboxMessageAsync();
        await host.RunProcessorUntilProcessedAsync(messageId);

        var state = host.Provider.GetRequiredService<FirstConsumerState>();
        Assert.Equal(2, state.Attempts);
        Assert.Equal(1, state.Failures);
        Assert.Equal(1, state.Successes);

        await using var scope = host.Provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var outboxMessage = await dbContext.OutboxMessages.AsNoTracking()
            .SingleAsync(message => message.Id == messageId);
        Assert.Equal(OutboxMessageStatus.Processed, outboxMessage.Status);
        Assert.Equal(1, outboxMessage.Attempts);

        var claim = Assert.Single(await dbContext.InboxMessages.AsNoTracking()
            .Where(message => message.MessageId == messageId)
            .ToListAsync());
        Assert.Equal(FirstTestConsumer.Consumer, claim.Consumer);
    }

    [Fact]
    public async Task MultipleConsumers_EachRunOnce_AcrossRedelivery()
    {
        await using var host = await OutboxTestHost.CreateAsync(
            services => RegisterConsumers(
                services,
                typeof(FirstTestConsumer),
                typeof(SecondTestConsumer)));

        var messageId = await host.CreateProductWithOutboxMessageAsync();
        await host.RunProcessorUntilProcessedAsync(messageId);

        await AssertConsumerResultsAsync(host, messageId);

        await host.RedeliverOutboxMessageAsync(messageId);
        await host.RunProcessorUntilProcessedAsync(messageId);

        await AssertConsumerResultsAsync(host, messageId);

        await using var scope = host.Provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var outboxMessage = await dbContext.OutboxMessages.AsNoTracking()
            .SingleAsync(message => message.Id == messageId);
        Assert.Equal(OutboxMessageStatus.Processed, outboxMessage.Status);
    }

    [Fact]
    public async Task PartialConsumerFailure_FirstConsumerIsNotRepeatedOnRetry()
    {
        await using var host = await OutboxTestHost.CreateAsync(
            services => RegisterConsumers(
                services,
                typeof(FirstTestConsumer),
                typeof(SecondTestConsumer)));

        host.Provider.GetRequiredService<SecondConsumerState>().FailuresBeforeSuccess = 1;

        var messageId = await host.CreateProductWithOutboxMessageAsync();
        await host.RunProcessorUntilProcessedAsync(messageId);

        var first = host.Provider.GetRequiredService<FirstConsumerState>();
        var second = host.Provider.GetRequiredService<SecondConsumerState>();

        Assert.Equal(1, first.Attempts);
        Assert.Equal(1, first.Successes);
        Assert.Equal(2, second.Attempts);
        Assert.Equal(1, second.Failures);
        Assert.Equal(1, second.Successes);

        await using var scope = host.Provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.Equal(
            2,
            await dbContext.InboxMessages.CountAsync(message => message.MessageId == messageId));

        var outboxMessage = await dbContext.OutboxMessages.AsNoTracking()
            .SingleAsync(message => message.Id == messageId);
        Assert.Equal(OutboxMessageStatus.Processed, outboxMessage.Status);
        Assert.Equal(1, outboxMessage.Attempts);
    }

    [Fact]
    public async Task ConcurrentClaims_ProduceOneWinner_WithoutDuplicateKeyException()
    {
        await using var host = await OutboxTestHost.CreateAsync();
        var messageId = Guid.NewGuid();

        await using var firstScope = host.Provider.CreateAsyncScope();
        await using var secondScope = host.Provider.CreateAsyncScope();

        var results = await Task.WhenAll(
            ClaimAsync(firstScope, messageId),
            ClaimAsync(secondScope, messageId));

        Assert.Equal(1, results.Count(claimed => claimed));

        await using var verifyScope = host.Provider.CreateAsyncScope();
        var dbContext = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(
            1,
            await dbContext.InboxMessages.CountAsync(message => message.MessageId == messageId));
    }

    [Fact]
    public async Task FollowUpOutbox_IsRolledBackWithTheClaim_AndWrittenOnceOnRetry()
    {
        await using var host = await OutboxTestHost.CreateAsync(
            services => RegisterConsumers(services, typeof(FollowUpConsumer)));

        host.Provider.GetRequiredService<FirstConsumerState>().FailuresBeforeSuccess = 1;

        var messageId = await host.CreateProductWithOutboxMessageAsync();
        await host.RunProcessorUntilProcessedAsync(messageId);

        var state = host.Provider.GetRequiredService<FirstConsumerState>();
        Assert.Equal(2, state.Attempts);
        Assert.Equal(1, state.Failures);
        Assert.Equal(1, state.Successes);

        await using var scope = host.Provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.Equal(
            1,
            await dbContext.OutboxMessages.CountAsync(
                message => message.Type.Contains(nameof(FollowUpNotification))));

        Assert.Equal(
            1,
            await dbContext.InboxMessages.CountAsync(
                message =>
                    message.MessageId == messageId &&
                    message.Consumer == FollowUpConsumer.Consumer));
    }

    [Fact]
    public async Task Cleanup_DeletesOnlyExpiredInboxRows()
    {
        await using var host = await OutboxTestHost.CreateAsync();

        var now = host.Provider.GetRequiredService<TimeProvider>().GetUtcNow();
        var oldMessageId = Guid.NewGuid();
        var recentMessageId = Guid.NewGuid();

        await using (var arrangeScope = host.Provider.CreateAsyncScope())
        {
            var dbContext = arrangeScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            dbContext.InboxMessages.Add(InboxMessage.Claim(
                oldMessageId,
                "tests.cleanup-old",
                "tests.cleanup-event",
                now.AddDays(-31)));

            dbContext.InboxMessages.Add(InboxMessage.Claim(
                recentMessageId,
                "tests.cleanup-recent",
                "tests.cleanup-event",
                now.AddDays(-1)));

            await dbContext.SaveChangesAsync();
        }

        var cleanupService = host.Provider.GetRequiredService<InboxCleanupService>();
        var deleted = await cleanupService.CleanupAsync();

        Assert.Equal(1, deleted);

        await using var assertScope = host.Provider.CreateAsyncScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.False(await assertDb.InboxMessages.AnyAsync(
            message => message.MessageId == oldMessageId));
        Assert.True(await assertDb.InboxMessages.AnyAsync(
            message => message.MessageId == recentMessageId));
    }

    [Fact]
    public async Task ClaimOutsideTransaction_ThrowsDocumentedException()
    {
        await using var host = await OutboxTestHost.CreateAsync();
        await using var scope = host.Provider.CreateAsyncScope();

        var inbox = scope.ServiceProvider.GetRequiredService<IInbox>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => inbox.TryClaimAsync(Guid.NewGuid(), "tests.event", "tests.consumer"));

        Assert.Contains("consumer transaction", exception.Message);
    }

    [Fact]
    public async Task Claim_ValidatesArguments()
    {
        await using var host = await OutboxTestHost.CreateAsync();
        await using var scope = host.Provider.CreateAsyncScope();

        var inbox = scope.ServiceProvider.GetRequiredService<IInbox>();

        await Assert.ThrowsAsync<ArgumentException>(
            () => inbox.TryClaimAsync(Guid.Empty, "tests.event", "tests.consumer"));

        await Assert.ThrowsAsync<ArgumentException>(
            () => inbox.TryClaimAsync(Guid.NewGuid(), " ", "tests.consumer"));

        await Assert.ThrowsAsync<ArgumentException>(
            () => inbox.TryClaimAsync(Guid.NewGuid(), "tests.event", " "));
    }

    [Fact]
    public async Task ExecuteInTransaction_CommitsChanges()
    {
        await using var host = await OutboxTestHost.CreateAsync();
        await using var scope = host.Provider.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var brandResult = ProductBrand.Create(
            new ProductBrandId(Guid.NewGuid()),
            $"Committed Brand {Guid.NewGuid():N}");

        Assert.True(brandResult.IsSuccess);

        await dbContext.ExecuteInTransactionAsync(async ct =>
        {
            dbContext.BrandsSet.Add(brandResult.Value);
            await dbContext.SaveChangesAsync(ct);
        });

        Assert.True(await dbContext.BrandsSet.AsNoTracking()
            .AnyAsync(brand => brand.Id == brandResult.Value.Id));
    }

    [Fact]
    public async Task ExecuteInTransaction_RollsBackChangesAndClearsTracker()
    {
        await using var host = await OutboxTestHost.CreateAsync();
        await using var scope = host.Provider.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var brandResult = ProductBrand.Create(
            new ProductBrandId(Guid.NewGuid()),
            $"Rolled Back Brand {Guid.NewGuid():N}");

        Assert.True(brandResult.IsSuccess);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => dbContext.ExecuteInTransactionAsync(async ct =>
            {
                dbContext.BrandsSet.Add(brandResult.Value);
                await dbContext.SaveChangesAsync(ct);

                throw new InvalidOperationException("Simulated handler failure.");
            }));

        Assert.Empty(dbContext.ChangeTracker.Entries());
        Assert.False(await dbContext.BrandsSet.AsNoTracking()
            .AnyAsync(brand => brand.Id == brandResult.Value.Id));
    }

    [Fact]
    public async Task ExecuteInTransaction_ThrowsWhenTransactionIsAlreadyActive()
    {
        await using var host = await OutboxTestHost.CreateAsync();
        await using var scope = host.Provider.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.ExecuteInTransactionAsync(async ct =>
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => dbContext.ExecuteInTransactionAsync(_ => Task.CompletedTask, ct));
        });
    }

    [Fact]
    public async Task Migration_BackfillsLegacyInboxRows_AndCreatesCompositeKey()
    {
        await using var host = await OutboxTestHost.CreateAsync(migrate: false);
        await using var scope = host.Provider.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var migrator = dbContext.Database.GetService<IMigrator>();

        var migrations = dbContext.Database.GetMigrations().ToList();
        var previousMigration = migrations[^2];

        await migrator.MigrateAsync(previousMigration);

        var messageId = Guid.NewGuid();

        await dbContext.Database.ExecuteSqlAsync($"""
            INSERT INTO InboxMessages (Id, EventType, ConsumedOnUtc)
            VALUES ({messageId}, {"tests.legacy-event"}, {DateTimeOffset.UtcNow});
            """);

        await migrator.MigrateAsync();

        var legacyRow = await dbContext.InboxMessages.AsNoTracking()
            .SingleAsync(message => message.MessageId == messageId);

        Assert.Equal("legacy", legacyRow.Consumer);
        Assert.Equal("tests.legacy-event", legacyRow.EventType);

        var primaryKeyColumns = await dbContext.Database
            .SqlQuery<string>($"""
                SELECT c.name AS Value
                FROM sys.indexes i
                INNER JOIN sys.index_columns ic
                    ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                INNER JOIN sys.columns c
                    ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                WHERE i.name = 'PK_InboxMessages'
                    AND i.object_id = OBJECT_ID('InboxMessages')
                ORDER BY ic.key_ordinal
                """)
            .ToListAsync();

        Assert.Equal(["MessageId", "Consumer"], primaryKeyColumns);

        var indexCount = await dbContext.Database
            .SqlQuery<int>($"""
                SELECT COUNT(*) AS Value
                FROM sys.indexes
                WHERE name = 'IX_InboxMessages_ConsumedOnUtc'
                    AND object_id = OBJECT_ID('InboxMessages')
                """)
            .SingleAsync();

        Assert.Equal(1, indexCount);
    }

    private static async Task AssertConsumerResultsAsync(OutboxTestHost host, Guid messageId)
    {
        var first = host.Provider.GetRequiredService<FirstConsumerState>();
        var second = host.Provider.GetRequiredService<SecondConsumerState>();

        Assert.Equal(1, first.Attempts);
        Assert.Equal(1, first.Successes);
        Assert.Equal(1, second.Attempts);
        Assert.Equal(1, second.Successes);

        await using var scope = host.Provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var claims = await dbContext.InboxMessages.AsNoTracking()
            .Where(message => message.MessageId == messageId)
            .OrderBy(message => message.Consumer)
            .ToListAsync();

        Assert.Equal(2, claims.Count);
        Assert.Contains(claims, claim => claim.Consumer == FirstTestConsumer.Consumer);
        Assert.Contains(claims, claim => claim.Consumer == SecondTestConsumer.Consumer);
    }

    private static async Task<bool> ClaimAsync(AsyncServiceScope scope, Guid messageId)
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var inbox = scope.ServiceProvider.GetRequiredService<IInbox>();

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var claimed = await inbox.TryClaimAsync(
            messageId,
            "tests.concurrent-event",
            "tests.concurrent-consumer");

        await transaction.CommitAsync();

        return claimed;
    }

    private static void RegisterConsumers(IServiceCollection services, params Type[] consumerTypes)
    {
        services.RemoveAll<INotificationHandler<ProductCreatedDomainEvent>>();

        services.AddSingleton(new FirstConsumerState());
        services.AddSingleton(new SecondConsumerState());

        foreach (var consumerType in consumerTypes)
        {
            services.AddTransient(
                typeof(INotificationHandler<ProductCreatedDomainEvent>),
                consumerType);
        }
    }
}

public sealed class FirstConsumerState
{
    public int FailuresBeforeSuccess { get; set; }
    public int Attempts;
    public int Failures;
    public int Successes;
}

public sealed class SecondConsumerState
{
    public int FailuresBeforeSuccess { get; set; }
    public int Attempts;
    public int Failures;
    public int Successes;
}

public sealed class FirstTestConsumer(
    IInbox inbox,
    IUnitOfWork unitOfWork,
    IOutboxMessageContext messageContext,
    ILogger<FirstTestConsumer> logger,
    FirstConsumerState state)
    : IdempotentNotificationHandler<ProductCreatedDomainEvent>(
        inbox,
        unitOfWork,
        messageContext,
        logger)
{
    public const string Consumer = "tests.consumer-a";

    protected override string ConsumerId => Consumer;

    protected override Task HandleCore(
        ProductCreatedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var attempt = Interlocked.Increment(ref state.Attempts);

        if (attempt <= state.FailuresBeforeSuccess)
        {
            Interlocked.Increment(ref state.Failures);
            throw new InvalidOperationException(
                $"{Consumer} failed on attempt {attempt}.");
        }

        Interlocked.Increment(ref state.Successes);

        return Task.CompletedTask;
    }
}

public sealed class SecondTestConsumer(
    IInbox inbox,
    IUnitOfWork unitOfWork,
    IOutboxMessageContext messageContext,
    ILogger<SecondTestConsumer> logger,
    SecondConsumerState state)
    : IdempotentNotificationHandler<ProductCreatedDomainEvent>(
        inbox,
        unitOfWork,
        messageContext,
        logger)
{
    public const string Consumer = "tests.consumer-b";

    protected override string ConsumerId => Consumer;

    protected override Task HandleCore(
        ProductCreatedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var attempt = Interlocked.Increment(ref state.Attempts);

        if (attempt <= state.FailuresBeforeSuccess)
        {
            Interlocked.Increment(ref state.Failures);
            throw new InvalidOperationException(
                $"{Consumer} failed on attempt {attempt}.");
        }

        Interlocked.Increment(ref state.Successes);

        return Task.CompletedTask;
    }
}

public sealed class FollowUpConsumer(
    IInbox inbox,
    IUnitOfWork unitOfWork,
    IOutboxMessageContext messageContext,
    IOutboxWriter outboxWriter,
    ILogger<FollowUpConsumer> logger,
    FirstConsumerState state)
    : IdempotentNotificationHandler<ProductCreatedDomainEvent>(
        inbox,
        unitOfWork,
        messageContext,
        logger)
{
    public const string Consumer = "tests.follow-up";

    protected override string ConsumerId => Consumer;

    protected override Task HandleCore(
        ProductCreatedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var attempt = Interlocked.Increment(ref state.Attempts);

        outboxWriter.Enqueue(new FollowUpNotification(notification.ProductId.Value));

        if (attempt <= state.FailuresBeforeSuccess)
        {
            Interlocked.Increment(ref state.Failures);
            throw new InvalidOperationException(
                $"{Consumer} failed on attempt {attempt}.");
        }

        Interlocked.Increment(ref state.Successes);

        return Task.CompletedTask;
    }
}

public sealed record FollowUpNotification(Guid ProductId) : INotification;
