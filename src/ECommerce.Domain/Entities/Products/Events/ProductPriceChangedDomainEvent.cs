using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities.Products.Events
{
    public sealed record ProductPriceChangedDomainEvent(ProductId ProductId, Money OldPrice, Money NewPrice) : IDomainEvent;
}