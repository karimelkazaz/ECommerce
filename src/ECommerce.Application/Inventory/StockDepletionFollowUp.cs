using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities.Inventory;
using ECommerce.Domain.Entities.Products;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Inventory;

internal static class StockDepletionFollowUp
{
    // Only Reserve and Adjust can drive availability to zero; Release only frees
    // stock, so its consumer never subscribes. The event payload proves the
    // product *reached* zero — re-reading the current state proves it *stayed*
    // there (an adjustment or release may have raced ahead of the consumer).
    public static async Task EnqueueIfDepletedAsync(
        InventoryItemId inventoryItemId,
        ProductId productId,
        int eventAvailableQuantity,
        IInventoryItemRepository inventory,
        IOutboxWriter outboxWriter,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (eventAvailableQuantity != 0)
        {
            return;
        }

        var inventoryItem = await inventory.GetByProductIdAsync(productId, cancellationToken);
        if (inventoryItem is null || inventoryItem.AvailableQuantity > 0)
        {
            logger.LogDebug(
                "Product {ProductId} is not currently depleted; skipping the depletion follow-up.",
                productId.Value);
            return;
        }

        // Database-only here — the external flow (restock alerts, merchandising)
        // subscribes to StockDepletedNotification, which is delivered once the
        // claim and the consuming transaction have committed.
        outboxWriter.Enqueue(new StockDepletedNotification(inventoryItemId.Value, productId.Value));

        logger.LogInformation(
            "Enqueued stock depletion follow-up for product {ProductId}.",
            productId.Value);
    }
}
