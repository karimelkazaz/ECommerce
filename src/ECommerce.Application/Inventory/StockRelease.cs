using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities.Inventory;
using ECommerce.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Inventory;

internal static class StockRelease
{
    // Checkout reserves stock synchronously via the inventory ledger; every
    // cancellation, refund or payment failure replays the outstanding reserve
    // entries back into stock. Orders that never reserved (e.g. created before
    // inventory existed) leave no ledger entries, so Release is a no-op for them.
    public static async Task ReleaseOrderStockAsync(
        IApplicationDbContext db,
        IInventoryItemRepository inventory,
        OrderId orderId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var productIds = await db.Orders
            .Where(o => o.Id == orderId)
            .SelectMany(o => o.OrderItems)
            .Select(i => i.ProductId)
            .ToListAsync(cancellationToken);

        foreach (var productId in productIds.Distinct())
        {
            var inventoryItem = await inventory.GetByProductIdAsync(productId, cancellationToken);
            if (inventoryItem is null)
            {
                logger.LogWarning(
                    "No inventory record for product {ProductId}; skipping stock release for order {OrderId}.",
                    productId.Value,
                    orderId.Value);
                continue;
            }

            var releaseResult = inventoryItem.Release(orderId.Value);
            if (releaseResult.IsFailure)
            {
                logger.LogWarning(
                    "Stock release failed for product {ProductId} on order {OrderId}: {Error}",
                    productId.Value,
                    orderId.Value,
                    releaseResult.Error.Description);
            }
        }
    }
}
