using ECommerce.Application.Common.EventHandlers;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Products.Events;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Products.EventHandlers;

public sealed class ProductPriceChangedDomainEventHandler(
    IInbox inbox,
    IUnitOfWork unitOfWork,
    IOutboxMessageContext messageContext,
    ILogger<ProductPriceChangedDomainEventHandler> logger)
    : IdempotentNotificationHandler<ProductPriceChangedDomainEvent>(inbox, unitOfWork, messageContext, logger)
{
    protected override string ConsumerId => "catalog.product-price-changed";

    protected override Task HandleCore(
        ProductPriceChangedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Product {ProductId} price changed from {OldAmount} {OldCurrency} to {NewAmount} {NewCurrency}.",
            notification.ProductId.Value,
            notification.OldPrice.Amount,
            notification.OldPrice.Currency.Code,
            notification.NewPrice.Amount,
            notification.NewPrice.Currency.Code);

        return Task.CompletedTask;
    }
}
