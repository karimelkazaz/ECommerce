using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities.ProductsBrand.Events;

public sealed record ProductBrandCreatedDomainEvent(ProductBrandId ProductBrandId) : IDomainEvent;