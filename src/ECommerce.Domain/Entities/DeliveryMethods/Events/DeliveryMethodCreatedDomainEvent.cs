using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities.DeliveryMethods.Events;

public sealed record DeliveryMethodCreatedDomainEvent(DeliveryMethodId DeliveryMethodId) : IDomainEvent;
