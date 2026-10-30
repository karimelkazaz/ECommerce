using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities.Orders.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Orders.EventHandlers;

public sealed class OrderRefundedDomainEventHandler(IInbox inbox, ILogger<OrderRefundedDomainEventHandler> logger)
    : INotificationHandler<OrderRefundedDomainEvent>
{
    public async Task Handle(OrderRefundedDomainEvent notification, CancellationToken cancellationToken)
    {
        if (!await inbox.TryClaimAsync(cancellationToken))
        {
            logger.LogInformation("Suppressed duplicate delivery of OrderRefundedDomainEvent.");
            return;
        }

        logger.LogInformation("Order {OrderId} was refunded.", notification.OrderId.Value);
    }
}
