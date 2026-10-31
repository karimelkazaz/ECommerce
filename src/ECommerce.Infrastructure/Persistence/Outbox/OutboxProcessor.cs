using System.Text.Json;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Infrastructure.Persistence.DbContexts;
using ECommerce.Infrastructure.Persistence.Locking;
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
    TimeProvider timeProvider,
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
        // The lock lives on its own session/connection so message transactions
        // never become entangled with the lock session.
        await using var lockScope = scopeFactory.CreateAsyncScope();
        var lockDbContext = lockScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await lockDbContext.Database.OpenConnectionAsync(stoppingToken);

        try
        {
            if (!await SqlApplicationLock.TryAcquireAsync(lockDbContext, LockResource, stoppingToken))
            {
                logger.LogDebug("Another instance holds the outbox tick lock; skipping this tick.");
                return;
            }

            try
            {
                var messageIds = await GetPendingMessageIdsAsync(stoppingToken);

                foreach (var messageId in messageIds)
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    await ProcessMessageAsync(messageId, stoppingToken);
                }
            }
            finally
            {
                try
                {
                    await SqlApplicationLock.ReleaseAsync(lockDbContext, LockResource);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
                        ex,
                        "Failed to explicitly release the outbox tick lock; it will be released automatically when the session ends.");
                }
            }
        }
        finally
        {
            await lockDbContext.Database.CloseConnectionAsync();
        }
    }

    private async Task<List<Guid>> GetPendingMessageIdsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await dbContext.OutboxMessages
            .AsNoTracking()
            .Where(message => message.Status == OutboxMessageStatus.Pending)
            .OrderBy(message => message.OccurredOnUtc)
            .Take(options.Value.BatchSize)
            .Select(message => message.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task ProcessMessageAsync(Guid messageId, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
            var messageContext = scope.ServiceProvider.GetRequiredService<OutboxMessageContext>();

            var message = await dbContext.OutboxMessages
                .SingleOrDefaultAsync(candidate => candidate.Id == messageId, cancellationToken);

            if (message is null || message.Status != OutboxMessageStatus.Pending)
            {
                return;
            }

            var eventType = Type.GetType(message.Type, throwOnError: false);

            if (eventType is null)
            {
                throw new InvalidOperationException($"Unknown event type '{message.Type}'.");
            }

            messageContext.Initialize(message.Id, eventType.FullName ?? eventType.Name);

            var domainEvent = (INotification)JsonSerializer.Deserialize(
                message.Content,
                eventType,
                OutboxJsonOptions.Default)!;

            // Each consumer opens and commits its own transaction, claims the
            // message for its own consumer ID, and saves its own changes.
            await publisher.Publish(domainEvent, cancellationToken);

            message.MarkProcessed(timeProvider.GetUtcNow());
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Published outbox message {MessageId} ({EventType}).",
                message.Id,
                eventType.Name);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await RecordFailureAsync(messageId, ex, cancellationToken);
        }
    }

    private async Task RecordFailureAsync(
        Guid messageId,
        Exception exception,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var message = await dbContext.OutboxMessages
                .SingleOrDefaultAsync(candidate => candidate.Id == messageId, cancellationToken);

            if (message is null)
            {
                return;
            }

            message.RecordFailure(exception.Message, options.Value.MaxAttempts);
            await dbContext.SaveChangesAsync(cancellationToken);

            if (message.Status == OutboxMessageStatus.Failed)
            {
                logger.LogError(
                    exception,
                    "Outbox message {MessageId} dead-lettered after {Attempts} attempts.",
                    message.Id,
                    message.Attempts);
            }
            else
            {
                logger.LogWarning(
                    exception,
                    "Failed to publish outbox message {MessageId} (attempt {Attempts}). Will retry.",
                    message.Id,
                    message.Attempts);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(
                ex,
                "Failed to record the failure of outbox message {MessageId}.",
                messageId);
        }
    }
}
