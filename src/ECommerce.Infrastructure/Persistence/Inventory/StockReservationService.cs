using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Inventory;
using ECommerce.Domain.Entities.Orders;
using ECommerce.Infrastructure.Persistence.DbContexts;
using ECommerce.Infrastructure.Persistence.Locking;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Persistence.Inventory;

internal sealed class StockReservationService(
    ApplicationDbContext db,
    IInventoryItemRepository inventory,
    IUnitOfWork unitOfWork,
    ILogger<StockReservationService> logger) : IStockReservationService
{
    // A single resource serializes every reservation; contention is one
    // checkout at a time, which is acceptable for stock-integrity-critical work.
    private const string LockResource = "inventory:reserve";

    public async Task<Result> ReserveForOrderAsync(
        OrderId orderId,
        IReadOnlyList<StockReservationLine> lines,
        CancellationToken cancellationToken = default)
    {
        if (lines.Count == 0)
            return Result.Success();

        Error? failure = null;
        var lockAcquired = false;

        try
        {
            await db.ExecuteInTransactionAsync(async ct =>
            {
                if (!await SqlApplicationLock.TryAcquireAsync(db, LockResource, ct))
                {
                    logger.LogWarning(
                        "Stock reservation lock could not be acquired for order {OrderId}.",
                        orderId.Value);
                    failure = InventoryErrors.LockUnavailable;
                    return;
                }

                lockAcquired = true;

                foreach (var group in lines.GroupBy(l => l.ProductId))
                {
                    var quantity = group.Sum(l => l.Quantity);

                    var inventoryItem = await inventory.GetByProductIdAsync(group.Key, ct);
                    if (inventoryItem is null)
                    {
                        logger.LogWarning(
                            "No inventory record for product {ProductId}; refusing checkout for order {OrderId}.",
                            group.Key.Value,
                            orderId.Value);
                        failure = Error.Conflict(
                            "Inventory.ProductNotTracked",
                            $"Product '{group.First().ProductName}' has no inventory record and cannot be purchased.");
                        return;
                    }

                    var reserveResult = inventoryItem.Reserve(orderId, quantity);
                    if (reserveResult.IsFailure)
                    {
                        logger.LogInformation(
                            "Insufficient stock for product {ProductId} on order {OrderId}.",
                            group.Key.Value,
                            orderId.Value);
                        failure = Error.Conflict(
                            "Inventory.InsufficientStock",
                            $"Only {inventoryItem.AvailableQuantity} left of '{group.First().ProductName}'. Please reduce the quantity.");
                        return;
                    }
                }

                // Persists the reservations together with everything already tracked
                // on the ambient context — the checkout order and its outbox events.
                // On a rowversion conflict this throws, so ExecuteInTransactionAsync
                // rolls back the whole transaction (order + reservation together).
                await unitOfWork.SaveChangesAsync(ct);
            }, cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(
                ex,
                "Stock changed concurrently for order {OrderId}; reserving must be retried.",
                orderId.Value);
            failure = InventoryErrors.ConcurrencyConflict;
        }

        if (lockAcquired)
        {
            try
            {
                // Released only after the commit; a lost release dies with the
                // session, it cannot leak into another request.
                await SqlApplicationLock.ReleaseAsync(db, LockResource);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to release the stock reservation lock.");
            }
        }

        return failure is null ? Result.Success() : Result.Failure(failure);
    }
}
