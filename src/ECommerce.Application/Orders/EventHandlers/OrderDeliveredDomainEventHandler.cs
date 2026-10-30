using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities.Orders.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Orders.EventHandlers;

public sealed class OrderDeliveredDomainEventHandler(IInbox inbox, ILogger<OrderDeliveredDomainEventHandler> logger)
    : INotificationHandler<OrderDeliveredDomainEvent>
{
    public async Task Handle(OrderDeliveredDomainEvent notification, CancellationToken cancellationToken)
    {
        if (!await inbox.TryClaimAsync(cancellationToken))
        {
            logger.LogInformation("Suppressed duplicate delivery of OrderDeliveredDomainEvent.");
            return;
        }

        logger.LogInformation("Order {OrderId} was delivered.", notification.OrderId.Value);
    }
}
