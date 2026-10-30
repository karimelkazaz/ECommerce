using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities.Orders.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Orders.EventHandlers;

public sealed class OrderShippedDomainEventHandler(IInbox inbox, ILogger<OrderShippedDomainEventHandler> logger)
    : INotificationHandler<OrderShippedDomainEvent>
{
    public async Task Handle(OrderShippedDomainEvent notification, CancellationToken cancellationToken)
    {
        if (!await inbox.TryClaimAsync(cancellationToken))
        {
            logger.LogInformation("Suppressed duplicate delivery of OrderShippedDomainEvent.");
            return;
        }

        logger.LogInformation("Order {OrderId} was shipped.", notification.OrderId.Value);
    }
}
