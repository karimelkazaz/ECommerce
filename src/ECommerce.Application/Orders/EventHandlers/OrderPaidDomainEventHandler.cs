using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities.Orders.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Orders.EventHandlers;

public sealed class OrderPaidDomainEventHandler(IInbox inbox, ILogger<OrderPaidDomainEventHandler> logger)
    : INotificationHandler<OrderPaidDomainEvent>
{
    public async Task Handle(OrderPaidDomainEvent notification, CancellationToken cancellationToken)
    {
        if (!await inbox.TryClaimAsync(cancellationToken))
        {
            logger.LogInformation("Suppressed duplicate delivery of OrderPaidDomainEvent.");
            return;
        }

        logger.LogInformation("Order {OrderId} was paid.", notification.OrderId.Value);
    }
}
