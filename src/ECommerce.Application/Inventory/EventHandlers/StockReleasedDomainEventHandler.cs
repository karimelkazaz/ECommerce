using ECommerce.Application.Common.EventHandlers;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Inventory.Events;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Inventory.EventHandlers;

public sealed class StockReleasedDomainEventHandler(
    IInbox inbox,
    IUnitOfWork unitOfWork,
    IOutboxMessageContext messageContext,
    ILogger<StockReleasedDomainEventHandler> logger)
    : IdempotentNotificationHandler<StockReleasedDomainEvent>(inbox, unitOfWork, messageContext, logger)
{
    protected override string ConsumerId => "inventory.stock-released";

    protected override Task HandleCore(
        StockReleasedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Stock released for order {OrderId}: {ReleasedQuantity} units of product {ProductId} back, {AvailableQuantity} now available.",
            notification.OrderId.Value,
            notification.ReleasedQuantity,
            notification.ProductId.Value,
            notification.AvailableQuantity);

        return Task.CompletedTask;
    }
}
