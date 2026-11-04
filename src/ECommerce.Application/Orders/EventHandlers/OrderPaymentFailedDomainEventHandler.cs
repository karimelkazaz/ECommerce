using ECommerce.Application.Common.EventHandlers;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Inventory;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Inventory;
using ECommerce.Domain.Entities.Orders.Events;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Orders.EventHandlers;

public sealed class OrderPaymentFailedDomainEventHandler(
    IApplicationDbContext db,
    IInventoryItemRepository inventory,
    IInbox inbox,
    IUnitOfWork unitOfWork,
    IOutboxMessageContext messageContext,
    ILogger<OrderPaymentFailedDomainEventHandler> logger)
    : IdempotentNotificationHandler<OrderPaymentFailedDomainEvent>(inbox, unitOfWork, messageContext, logger)
{
    protected override string ConsumerId => "orders.order-payment-failed";

    protected override Task HandleCore(
        OrderPaymentFailedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Payment failed for order {OrderId}.",
            notification.OrderId.Value);

        // The order transitions to Cancelled via MarkAsPaymentFailed, which does
        // not raise OrderCancelledDomainEvent — release the reservation here.
        return StockRelease.ReleaseOrderStockAsync(db, inventory, notification.OrderId, logger, cancellationToken);
    }
}
