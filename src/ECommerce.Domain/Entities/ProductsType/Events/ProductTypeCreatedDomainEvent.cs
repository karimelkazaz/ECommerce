using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities.ProductsType.Events;

public sealed record ProductTypeCreatedDomainEvent(ProductTypeId ProductTypeId) : IDomainEvent;
