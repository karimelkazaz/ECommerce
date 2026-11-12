using ECommerce.Domain.Entities.Orders;
using ECommerce.Infrastructure.Persistence.DbContexts;
using ECommerce.Infrastructure.Persistence.Locking;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Persistence.OrderExpiry;

public sealed class OrderExpiryService(
    IServiceScopeFactory scopeFactory,
    IOptions<OrderExpiryOptions> options,
    TimeProvider timeProvider,
    ILogger<OrderExpiryService> logger) : BackgroundService
{
    private const string LockResource = "ECommerceDb:OrderExpiry";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(options.Value.ScanIntervalMinutes, 1));

        try
        {
            using var timer = new PeriodicTimer(interval);

            do
            {
                try
                {
                    await ExpireStaleOrdersAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Order expiry run failed.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Order expiry service stopped.");
        }
    }

    /// <summary>
    /// Runs one expiry pass. Returns the number of expired orders.
    /// </summary>
    public async Task<int> ExpireStaleOrdersAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            if (!await SqlApplicationLock.TryAcquireAsync(dbContext, LockResource, cancellationToken))
            {
                logger.LogDebug("Another instance holds the order expiry lock; skipping this run.");
                return 0;
            }

            try
            {
                var cutoff = timeProvider.GetUtcNow().AddHours(-options.Value.PendingExpirationHours);

                var staleOrders = await dbContext.OrdersSet
                    .Where(order => order.Status == OrderStatus.Pending && order.CreatedOnUtc < cutoff)
                    .ToListAsync(cancellationToken);

                if (staleOrders.Count == 0)
                {
                    logger.LogDebug("No pending orders older than {Cutoff} were found.", cutoff);
                    return 0;
                }

                var reason = $"Expired — payment was not completed within {options.Value.PendingExpirationHours} hours.";

                foreach (var order in staleOrders)
                {
                    var result = order.CancelOrder(reason);
                    if (result.IsFailure)
                    {
                        // Only possible if a concurrent request cancelled the
                        // order first; the next scan sees the new state.
                        logger.LogWarning(
                            "Could not expire order {OrderId}: {Error}",
                            order.Id.Value,
                            result.Error.Description);
                    }
                }

                // The OutboxInterceptor persists the raised events atomically
                // with the status changes; the processor then releases stock
                // and enqueues cancellation emails through the usual flow.
                await dbContext.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    "Expired {Count} pending orders last created before {Cutoff}.",
                    staleOrders.Count,
                    cutoff);

                return staleOrders.Count;
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
                        "Failed to explicitly release the order expiry lock; it will be released when the session ends.");
                }
            }
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }
}
