using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities.DeliveryMethods.Events;

public sealed record DeliveryMethodAvailabilityChangedDomainEvent(
    DeliveryMethodId DeliveryMethodId,
    bool IsAvailable) : IDomainEvent;
