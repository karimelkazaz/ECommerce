using ECommerce.Application.Common.EventHandlers;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Orders.Events;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Orders.EventHandlers;

public sealed class OrderCreatedDomainEventHandler(
    IInbox inbox,
    IUnitOfWork unitOfWork,
    IOutboxMessageContext messageContext,
    ILogger<OrderCreatedDomainEventHandler> logger)
    : IdempotentNotificationHandler<OrderCreatedDomainEvent>(inbox, unitOfWork, messageContext, logger)
{
    protected override string ConsumerId => "orders.order-created";

    protected override Task HandleCore(
        OrderCreatedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Order {OrderId} was created.", notification.OrderId.Value);

        return Task.CompletedTask;
    }
}
