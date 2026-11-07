using ECommerce.Application.Common.EventHandlers;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Orders;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Orders;
using ECommerce.Domain.Entities.Orders.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Orders.EventHandlers;

public sealed class OrderCreatedDomainEventHandler(
    IInbox inbox,
    IUnitOfWork unitOfWork,
    IOutboxMessageContext messageContext,
    IApplicationDbContext db,
    IIdentityService identity,
    IOutboxWriter outboxWriter,
    ILogger<OrderCreatedDomainEventHandler> logger)
    : IdempotentNotificationHandler<OrderCreatedDomainEvent>(inbox, unitOfWork, messageContext, logger)
{
    protected override string ConsumerId => "orders.order-created";

    protected override async Task HandleCore(
        OrderCreatedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Order {OrderId} was created.", notification.OrderId.Value);

        // Snapshot the order + recipient at claim time so the sender consumer
        // formats and delivers without re-reading aggregates.
        var order = await db.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == notification.OrderId, cancellationToken);

        if (order is null)
        {
            logger.LogWarning(
                "Created order {OrderId} not found; confirmation email skipped.",
                notification.OrderId.Value);
            return;
        }

        var profileResult = await identity.GetUserProfileAsync(order.UserId, cancellationToken);
        if (profileResult.IsFailure)
        {
            logger.LogWarning(
                "No profile found for the buyer of order {OrderId}: {Error}; confirmation email skipped.",
                notification.OrderId.Value,
                profileResult.Error.Code);
            return;
        }

        outboxWriter.Enqueue(new OrderConfirmationEmailNotification(
            notification.OrderId,
            profileResult.Value.Email,
            profileResult.Value.FirstName,
            order.OrderItems
                .Select(i => new EmailLine(i.ProductName, i.Quantity, i.Price.Amount))
                .ToList(),
            order.SubTotal.Amount,
            order.ShippingFee.Amount,
            order.Total.Amount,
            order.Total.Currency.Code));
    }
}
