using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities.Orders.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Orders.EventHandlers;

public sealed class OrderCancelledDomainEventHandler(IInbox inbox, ILogger<OrderCancelledDomainEventHandler> logger)
    : INotificationHandler<OrderCancelledDomainEvent>
{
    public async Task Handle(OrderCancelledDomainEvent notification, CancellationToken cancellationToken)
    {
        if (!await inbox.TryClaimAsync(cancellationToken))
        {
            logger.LogInformation("Suppressed duplicate delivery of OrderCancelledDomainEvent.");
            return;
        }

        logger.LogInformation("Order {OrderId} was cancelled.", notification.OrderId.Value);
    }
}
