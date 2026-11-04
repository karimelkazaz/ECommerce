using ECommerce.Application.Common.EventHandlers;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Inventory;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Inventory;
using ECommerce.Domain.Entities.Orders;
using ECommerce.Domain.Entities.Orders.Events;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Orders.EventHandlers;

public sealed class OrderCancelledDomainEventHandler(
    IApplicationDbContext db,
    IInventoryItemRepository inventory,
    IInbox inbox,
    IUnitOfWork unitOfWork,
    IOutboxMessageContext messageContext,
    ILogger<OrderCancelledDomainEventHandler> logger)
    : IdempotentNotificationHandler<OrderCancelledDomainEvent>(inbox, unitOfWork, messageContext, logger)
{
    protected override string ConsumerId => "orders.order-cancelled";

    protected override Task HandleCore(
        OrderCancelledDomainEvent notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Order {OrderId} was cancelled.", notification.OrderId.Value);

        return StockRelease.ReleaseOrderStockAsync(db, inventory, notification.OrderId, logger, cancellationToken);
    }
}
