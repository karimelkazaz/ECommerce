using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities.ProductsType.Events;

public sealed record ProductTypeRenamedDomainEvent(ProductTypeId ProductTypeId) : IDomainEvent;
