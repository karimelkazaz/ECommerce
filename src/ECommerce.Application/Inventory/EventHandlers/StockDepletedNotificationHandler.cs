using ECommerce.Application.Common.EventHandlers;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Inventory.EventHandlers;

public sealed class StockDepletedNotificationHandler(
    IInbox inbox,
    IUnitOfWork unitOfWork,
    IOutboxMessageContext messageContext,
    ILogger<StockDepletedNotificationHandler> logger)
    : IdempotentNotificationHandler<StockDepletedNotification>(inbox, unitOfWork, messageContext, logger)
{
    protected override string ConsumerId => "inventory.stock-depleted";

    protected override Task HandleCore(
        StockDepletedNotification notification,
        CancellationToken cancellationToken)
    {
        // Hook for exhaustion-sensitive follow-ups (restock alerts,
        // merchandising rules). Any external call made here must go out through
        // a follow-up message of its own and use the
        // {MessageId}:{ConsumerId} key from IOutboxMessageContext for
        // idempotency at the remote endpoint.
        logger.LogInformation(
            "Product {ProductId} ran out of stock (inventory item {InventoryItemId}).",
            notification.ProductId,
            notification.InventoryItemId);

        return Task.CompletedTask;
    }
}
