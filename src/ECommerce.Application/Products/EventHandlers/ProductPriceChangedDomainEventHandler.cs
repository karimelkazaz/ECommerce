using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities.Products.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Products.EventHandlers;

public sealed class ProductPriceChangedDomainEventHandler(IInbox inbox, ILogger<ProductPriceChangedDomainEventHandler> logger)
    : INotificationHandler<ProductPriceChangedDomainEvent>
{
    public async Task Handle(ProductPriceChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        if (!await inbox.TryClaimAsync(cancellationToken))
        {
            logger.LogInformation("Suppressed duplicate delivery of ProductPriceChangedDomainEvent.");
            return;
        }

        logger.LogInformation(
            "Product {ProductId} price changed from {OldAmount} {OldCurrency} to {NewAmount} {NewCurrency}.",
            notification.ProductId.Value,
            notification.OldPrice.Amount,
            notification.OldPrice.Currency.Code,
            notification.NewPrice.Amount,
            notification.NewPrice.Currency.Code);
    }
}
