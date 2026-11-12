using ECommerce.Application.Common.EventHandlers;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Inventory;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Inventory;
using ECommerce.Domain.Entities.Orders;
using ECommerce.Domain.Entities.Orders.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Orders.EventHandlers;

public sealed class OrderCancelledDomainEventHandler(
    IApplicationDbContext db,
    IInventoryItemRepository inventory,
    IIdentityService identity,
    IOutboxWriter outboxWriter,
    IInbox inbox,
    IUnitOfWork unitOfWork,
    IOutboxMessageContext messageContext,
    ILogger<OrderCancelledDomainEventHandler> logger)
    : IdempotentNotificationHandler<OrderCancelledDomainEvent>(inbox, unitOfWork, messageContext, logger)
{
    protected override string ConsumerId => "orders.order-cancelled";

    protected override async Task HandleCore(
        OrderCancelledDomainEvent notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Order {OrderId} was cancelled.", notification.OrderId.Value);

        await StockRelease.ReleaseOrderStockAsync(db, inventory, notification.OrderId, logger, cancellationToken);

        await EnqueueCancellationEmailAsync(notification, cancellationToken);
    }

    private async Task EnqueueCancellationEmailAsync(
        OrderCancelledDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .FirstOrDefaultAsync(o => o.Id == notification.OrderId, cancellationToken);

        if (order is null)
        {
            logger.LogWarning(
                "Cancelled order {OrderId} not found; cancellation email skipped.",
                notification.OrderId.Value);
            return;
        }

        var profileResult = await identity.GetUserProfileAsync(order.UserId, cancellationToken);
        if (profileResult.IsFailure)
        {
            logger.LogWarning(
                "No profile found for the buyer of order {OrderId}: {Error}; cancellation email skipped.",
                notification.OrderId.Value,
                profileResult.Error.Code);
            return;
        }
        
        outboxWriter.Enqueue(new OrderCancelledEmailNotification(
            notification.OrderId,
            profileResult.Value.Email,
            profileResult.Value.FirstName,
            order.Total.Amount,
            order.Total.Currency.Code,
            order.CancellationReason));
    }
}
