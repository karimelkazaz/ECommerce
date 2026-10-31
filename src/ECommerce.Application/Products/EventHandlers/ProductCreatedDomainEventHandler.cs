using ECommerce.Application.Common.EventHandlers;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Products.Events;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Products.EventHandlers;

public sealed class ProductCreatedDomainEventHandler(
    IInbox inbox,
    IUnitOfWork unitOfWork,
    IOutboxMessageContext messageContext,
    ILogger<ProductCreatedDomainEventHandler> logger)
    : IdempotentNotificationHandler<ProductCreatedDomainEvent>(inbox, unitOfWork, messageContext, logger)
{
    protected override string ConsumerId => "catalog.product-created";

    protected override Task HandleCore(
        ProductCreatedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Product {ProductId} was created.", notification.ProductId.Value);

        return Task.CompletedTask;
    }
}
