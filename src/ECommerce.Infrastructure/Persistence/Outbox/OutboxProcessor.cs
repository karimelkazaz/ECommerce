using System.Text.Json;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Infrastructure.Persistence.DbContexts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Persistence.Outbox;

public sealed class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    private const string LockResource = "ECommerceDb:OutboxProcessor";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollInterval = TimeSpan.FromSeconds(Math.Max(options.Value.PollIntervalSeconds, 1));

        try
        {
            using var timer = new PeriodicTimer(pollInterval);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await ProcessPendingMessagesAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Outbox processing tick failed.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Outbox processor stopped.");
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken stoppingToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
        var inbox = scope.ServiceProvider.GetRequiredService<IInbox>();

        await dbContext.Database.OpenConnectionAsync(stoppingToken);
        try
        {
            if (!await TryAcquireTickLockAsync(dbContext, stoppingToken))
            {
                logger.LogDebug("Another instance holds the outbox tick lock; skipping this tick.");
                return;
            }

            try
            {
                await ProcessBatchAsync(dbContext, publisher, inbox, stoppingToken);
            }
            finally
            {
                await ReleaseTickLockAsync(dbContext);
            }
        }
        finally
        {
            dbContext.Database.CloseConnection();
        }
    }

    private async Task<bool> TryAcquireTickLockAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var results = await dbContext.Database.SqlQuery<int>($@"
DECLARE @result int;
EXEC @result = sp_getapplock
    @Resource = {LockResource},
    @LockMode = 'Exclusive',
    @LockOwner = 'Session',
    @LockTimeout = 0;
SELECT @result AS Value;")
            .ToListAsync(cancellationToken);

        return results.Single() >= 0;
    }

    private async Task ReleaseTickLockAsync(ApplicationDbContext dbContext)
    {
        try
        {
            await dbContext.Database.ExecuteSqlAsync(
                $"EXEC sp_releaseapplock @Resource = {LockResource}, @LockOwner = 'Session';");
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to explicitly release the outbox tick lock; it will be released automatically when the session ends.");
        }
    }

    private async Task ProcessBatchAsync(ApplicationDbContext dbContext, IPublisher publisher, IInbox inbox, CancellationToken cancellationToken)
    {
        var pendingMessages = await dbContext.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Pending)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(options.Value.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in pendingMessages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var eventType = Type.GetType(message.Type, throwOnError: false);

                if (eventType is null)
                {
                    throw new InvalidOperationException($"Unknown event type '{message.Type}'.");
                }

                inbox.BeginScope(message.Id, eventType.FullName ?? eventType.Name);

                var domainEvent = (INotification)JsonSerializer.Deserialize(message.Content, eventType, OutboxJsonOptions.Default)!;

                await publisher.Publish(domainEvent, cancellationToken);

                message.MarkProcessed(DateTimeOffset.UtcNow);

                logger.LogInformation("Published outbox message {MessageId} ({EventType}).", message.Id, eventType.Name);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                message.RecordFailure(ex.Message, options.Value.MaxAttempts);

                if (message.Status == OutboxMessageStatus.Failed)
                {
                    logger.LogError(
                        ex,
                        "Outbox message {MessageId} dead-lettered after {Attempts} attempts.",
                        message.Id,
                        message.Attempts);
                }
                else
                {
                    logger.LogWarning(
                        ex,
                        "Failed to publish outbox message {MessageId} (attempt {Attempts}). Will retry.",
                        message.Id,
                        message.Attempts);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
