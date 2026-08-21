using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities.Orders.Events;

public sealed record OrderDeliveredDomainEvent(OrderId OrderId) : IDomainEvent;

