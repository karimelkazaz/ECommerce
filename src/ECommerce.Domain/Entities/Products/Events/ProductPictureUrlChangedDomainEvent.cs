using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Products;

namespace ECommerce.Domain.Entities.Products.Events
{
    public sealed record ProductPictureUrlChangedDomainEvent(ProductId ProductId, string NewPictureUrl) : IDomainEvent;
}
