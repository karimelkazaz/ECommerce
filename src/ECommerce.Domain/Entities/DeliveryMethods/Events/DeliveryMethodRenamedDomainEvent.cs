using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities.DeliveryMethods.Events;

public sealed record DeliveryMethodRenamedDomainEvent(DeliveryMethodId DeliveryMethodId) : IDomainEvent;
