using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities.Inventory;
using ECommerce.Domain.Entities.Products;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Inventory;

internal static class StockAttentionFollowUps
{
    // Only Reserve and Adjust can lower availability; Release only frees stock,
    // so its consumer never subscribes. The event payload proves the product
    // *reached* a level — re-reading the current state proves it *stayed* there
    // (an adjustment or release may have raced ahead of the consumer). Alert
    // levels are exclusive: a depleted product emits only StockDepleted.
    public static async Task EnqueueStockAttentionAsync(
        InventoryItemId inventoryItemId,
        ProductId productId,
        int eventAvailableQuantity,
        IInventoryItemRepository inventory,
        IStockAttentionPolicy policy,
        IOutboxWriter outboxWriter,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (eventAvailableQuantity == 0)
        {
            var depletedItem = await inventory.GetByProductIdAsync(productId, cancellationToken);
            if (depletedItem is null || depletedItem.AvailableQuantity > 0)
            {
                logger.LogDebug(
                    "Product {ProductId} is not currently depleted; skipping the depletion follow-up.",
                    productId.Value);
                return;
            }

            // Database-only here — the external flow (restock alerts,
            // merchandising) subscribes to StockDepletedNotification, which is
            // delivered once the claim and the consuming transaction commit.
            outboxWriter.Enqueue(new StockDepletedNotification(inventoryItemId.Value, productId.Value));

            logger.LogInformation(
                "Enqueued stock depletion follow-up for product {ProductId}.",
                productId.Value);
            return;
        }

        if (!policy.LowStockAlertsEnabled || eventAvailableQuantity > policy.LowStockThreshold)
        {
            return;
        }

        var item = await inventory.GetByProductIdAsync(productId, cancellationToken);
        if (item is null || item.AvailableQuantity > policy.LowStockThreshold)
        {
            logger.LogDebug(
                "Product {ProductId} is not currently below the low-stock threshold; skipping the low-stock follow-up.",
                productId.Value);
            return;
        }

        outboxWriter.Enqueue(new StockLowNotification(
            inventoryItemId.Value,
            productId.Value,
            eventAvailableQuantity));

        logger.LogInformation(
            "Enqueued low-stock follow-up for product {ProductId} ({AvailableQuantity} available).",
            productId.Value,
            eventAvailableQuantity);
    }
}
