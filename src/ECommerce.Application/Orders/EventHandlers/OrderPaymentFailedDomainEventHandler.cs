using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities.Orders.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Orders.EventHandlers;

public sealed class OrderPaymentFailedDomainEventHandler(IInbox inbox, ILogger<OrderPaymentFailedDomainEventHandler> logger)
    : INotificationHandler<OrderPaymentFailedDomainEvent>
{
    public async Task Handle(OrderPaymentFailedDomainEvent notification, CancellationToken cancellationToken)
    {
        if (!await inbox.TryClaimAsync(cancellationToken))
        {
            logger.LogInformation("Suppressed duplicate delivery of OrderPaymentFailedDomainEvent.");
            return;
        }

        logger.LogInformation("Payment failed for order {OrderId}.", notification.OrderId.Value);
    }
}
