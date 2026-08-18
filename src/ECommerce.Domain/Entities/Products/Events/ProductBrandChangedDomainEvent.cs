using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.Entities.ProductsBrand;

namespace ECommerce.Domain.Entities.Products.Events
{
    public sealed record ProductBrandChangedDomainEvent(ProductId ProductId, ProductBrandId NewBrandId) : IDomainEvent;
}
