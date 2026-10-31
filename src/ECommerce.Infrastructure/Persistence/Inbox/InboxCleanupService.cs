using ECommerce.Infrastructure.Persistence.DbContexts;
using ECommerce.Infrastructure.Persistence.Locking;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Persistence.Inbox;

/// <summary>
/// Deletes inbox rows older than <see cref="InboxOptions.RetentionDays"/>.
/// Retention must be longer than the outbox retry/dead-letter/replay window;
/// otherwise replaying an old message can execute a consumer again.
/// </summary>
public sealed class InboxCleanupService(
    IServiceScopeFactory scopeFactory,
    IOptions<InboxOptions> options,
    TimeProvider timeProvider,
    ILogger<InboxCleanupService> logger) : BackgroundService
{
    private const string LockResource = "ECommerceDb:InboxCleanup";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(options.Value.CleanupIntervalMinutes, 1));

        try
        {
            using var timer = new PeriodicTimer(interval);

            do
            {
                try
                {
                    await CleanupAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Inbox cleanup run failed.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Inbox cleanup service stopped.");
        }
    }

    /// <summary>
    /// Runs one cleanup pass. Returns the number of deleted rows.
    /// </summary>
    public async Task<int> CleanupAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            if (!await SqlApplicationLock.TryAcquireAsync(dbContext, LockResource, cancellationToken))
            {
                logger.LogDebug("Another instance holds the inbox cleanup lock; skipping this run.");
                return 0;
            }

            try
            {
                var cutoff = timeProvider.GetUtcNow().AddDays(-options.Value.RetentionDays);

                var deleted = await dbContext.InboxMessages
                    .Where(message => message.ConsumedOnUtc < cutoff)
                    .ExecuteDeleteAsync(cancellationToken);

                if (deleted > 0)
                {
                    logger.LogInformation(
                        "Deleted {Count} inbox rows consumed before {Cutoff}.",
                        deleted,
                        cutoff);
                }
                else
                {
                    logger.LogDebug("No inbox rows older than {Cutoff} were found.", cutoff);
                }

                return deleted;
            }
            finally
            {
                try
                {
                    await SqlApplicationLock.ReleaseAsync(dbContext, LockResource);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
                        ex,
                        "Failed to explicitly release the inbox cleanup lock; it will be released when the session ends.");
                }
            }
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }
}
