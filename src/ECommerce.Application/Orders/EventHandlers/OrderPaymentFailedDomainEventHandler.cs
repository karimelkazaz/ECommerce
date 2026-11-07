using ECommerce.Application.Common.EventHandlers;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Inventory;
using ECommerce.Application.Orders;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Inventory;
using ECommerce.Domain.Entities.Orders;
using ECommerce.Domain.Entities.Orders.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Orders.EventHandlers;

public sealed class OrderPaymentFailedDomainEventHandler(
    IApplicationDbContext db,
    IInventoryItemRepository inventory,
    IIdentityService identity,
    IOutboxWriter outboxWriter,
    IInbox inbox,
    IUnitOfWork unitOfWork,
    IOutboxMessageContext messageContext,
    ILogger<OrderPaymentFailedDomainEventHandler> logger)
    : IdempotentNotificationHandler<OrderPaymentFailedDomainEvent>(inbox, unitOfWork, messageContext, logger)
{
    protected override string ConsumerId => "orders.order-payment-failed";

    protected override async Task HandleCore(
        OrderPaymentFailedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Payment failed for order {OrderId}.",
            notification.OrderId.Value);

        // The order transitions to Cancelled via MarkAsPaymentFailed, which does
        // not raise OrderCancelledDomainEvent — release the reservation here.
        await StockRelease.ReleaseOrderStockAsync(db, inventory, notification.OrderId, logger, cancellationToken);

        await EnqueuePaymentFailedEmailAsync(notification, cancellationToken);
    }

    private async Task EnqueuePaymentFailedEmailAsync(
        OrderPaymentFailedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .FirstOrDefaultAsync(o => o.Id == notification.OrderId, cancellationToken);

        if (order is null)
        {
            logger.LogWarning(
                "Payment-failed order {OrderId} not found; recovery email skipped.",
                notification.OrderId.Value);
            return;
        }

        var profileResult = await identity.GetUserProfileAsync(order.UserId, cancellationToken);
        if (profileResult.IsFailure)
        {
            logger.LogWarning(
                "No profile found for the buyer of order {OrderId}: {Error}; recovery email skipped.",
                notification.OrderId.Value,
                profileResult.Error.Code);
            return;
        }

        // Snapshot the order + recipient so the sender consumer formats and
        // delivers without re-reading aggregates.
        outboxWriter.Enqueue(new OrderPaymentFailedEmailNotification(
            notification.OrderId,
            profileResult.Value.Email,
            profileResult.Value.FirstName,
            order.Total.Amount,
            order.Total.Currency.Code));
    }
}
