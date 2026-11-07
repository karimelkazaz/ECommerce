using ECommerce.Application.Common.EventHandlers;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Inventory;
using ECommerce.Domain.Entities.Inventory.Events;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Inventory.EventHandlers;

public sealed class StockAdjustedDomainEventHandler(
    IInbox inbox,
    IUnitOfWork unitOfWork,
    IOutboxMessageContext messageContext,
    IInventoryItemRepository inventory,
    IStockAttentionPolicy policy,
    IOutboxWriter outboxWriter,
    ILogger<StockAdjustedDomainEventHandler> logger)
    : IdempotentNotificationHandler<StockAdjustedDomainEvent>(inbox, unitOfWork, messageContext, logger)
{
    protected override string ConsumerId => "inventory.stock-adjusted";

    protected override async Task HandleCore(
        StockAdjustedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Stock adjusted for product {ProductId}: {AdjustedDelta} units ({AvailableQuantity} now available).",
            notification.ProductId.Value,
            notification.AdjustedDelta,
            notification.AvailableQuantity);

        await StockAttentionFollowUps.EnqueueStockAttentionAsync(
            notification.InventoryItemId,
            notification.ProductId,
            notification.AvailableQuantity,
            inventory,
            policy,
            outboxWriter,
            logger,
            cancellationToken);
    }
}
