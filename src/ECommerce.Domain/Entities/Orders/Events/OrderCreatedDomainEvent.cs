using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities.Orders.Events;

public sealed record OrderCreatedDomainEvent(OrderId OrderId) : IDomainEvent;

