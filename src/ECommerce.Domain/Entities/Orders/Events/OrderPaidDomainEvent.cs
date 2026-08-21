using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities.Orders.Events;

public sealed record OrderPaidDomainEvent(OrderId OrderId) : IDomainEvent;

