using ECommerce.Application.Common.EventHandlers;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Inventory;
using ECommerce.Domain.Entities.Inventory.Events;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Inventory.EventHandlers;

public sealed class StockReservedDomainEventHandler(
    IInbox inbox,
    IUnitOfWork unitOfWork,
    IOutboxMessageContext messageContext,
    IInventoryItemRepository inventory,
    IOutboxWriter outboxWriter,
    ILogger<StockReservedDomainEventHandler> logger)
    : IdempotentNotificationHandler<StockReservedDomainEvent>(inbox, unitOfWork, messageContext, logger)
{
    protected override string ConsumerId => "inventory.stock-reserved";

    protected override async Task HandleCore(
        StockReservedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Stock reserved for order {OrderId}: {ReservedQuantity} units of product {ProductId}, {AvailableQuantity} still available.",
            notification.OrderId.Value,
            notification.ReservedQuantity,
            notification.ProductId.Value,
            notification.AvailableQuantity);

        await StockDepletionFollowUp.EnqueueIfDepletedAsync(
            notification.InventoryItemId,
            notification.ProductId,
            notification.AvailableQuantity,
            inventory,
            outboxWriter,
            logger,
            cancellationToken);
    }
}
