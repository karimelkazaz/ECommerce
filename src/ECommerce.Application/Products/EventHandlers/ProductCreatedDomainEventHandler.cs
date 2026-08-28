using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities.Products.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Products.EventHandlers;

public sealed class ProductCreatedDomainEventHandler(IInbox inbox, ILogger<ProductCreatedDomainEventHandler> logger)
    : INotificationHandler<ProductCreatedDomainEvent>
{
    public async Task Handle(ProductCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        if (!await inbox.TryClaimAsync(cancellationToken))
        {
            logger.LogInformation("Suppressed duplicate delivery of ProductCreatedDomainEvent.");
            return;
        }

        logger.LogInformation("Product {ProductId} was created.", notification.ProductId.Value);
    }
}
