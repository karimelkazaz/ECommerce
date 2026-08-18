using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.Entities.ProductsType;

namespace ECommerce.Domain.Entities.Products.Events
{
    public sealed record ProductTypeChangedDomainEvent(ProductId ProductId, ProductTypeId NewTypeId) : IDomainEvent;
}
