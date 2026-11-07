using ECommerce.Application.Common.EventHandlers;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Inventory.EventHandlers;

public sealed class StockLowNotificationHandler(
    IInbox inbox,
    IUnitOfWork unitOfWork,
    IOutboxMessageContext messageContext,
    ILogger<StockLowNotificationHandler> logger)
    : IdempotentNotificationHandler<StockLowNotification>(inbox, unitOfWork, messageContext, logger)
{
    protected override string ConsumerId => "inventory.stock-low";

    protected override Task HandleCore(
        StockLowNotification notification,
        CancellationToken cancellationToken)
    {
        // Hook for low-stock follow-ups (ops dashboard, merchandising). Any
        // external call made here must go out through a follow-up message of
        // its own and use the {MessageId}:{ConsumerId} key from
        // IOutboxMessageContext for idempotency at the remote endpoint.
        logger.LogInformation(
            "Product {ProductId} is low on stock: {AvailableQuantity} available.",
            notification.ProductId,
            notification.AvailableQuantity);

        return Task.CompletedTask;
    }
}
