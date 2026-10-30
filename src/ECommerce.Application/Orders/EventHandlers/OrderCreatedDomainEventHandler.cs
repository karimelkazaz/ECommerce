using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities.Orders.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Orders.EventHandlers;

public sealed class OrderCreatedDomainEventHandler(IInbox inbox, ILogger<OrderCreatedDomainEventHandler> logger)
    : INotificationHandler<OrderCreatedDomainEvent>
{
    public async Task Handle(OrderCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        if (!await inbox.TryClaimAsync(cancellationToken))
        {
            logger.LogInformation("Suppressed duplicate delivery of OrderCreatedDomainEvent.");
            return;
        }

        logger.LogInformation("Order {OrderId} was created.", notification.OrderId.Value);
    }
}
