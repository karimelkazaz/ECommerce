using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities.DeliveryMethods.Events;

public sealed record DeliveryMethodDescriptionChangedDomainEvent(DeliveryMethodId DeliveryMethodId) : IDomainEvent;
