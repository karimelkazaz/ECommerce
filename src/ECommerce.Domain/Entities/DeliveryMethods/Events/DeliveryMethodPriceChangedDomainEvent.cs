using ECommerce.Domain.Common;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities.DeliveryMethods.Events;

public sealed record DeliveryMethodPriceChangedDomainEvent(
    DeliveryMethodId DeliveryMethodId,
    Money OldPrice,
    Money NewPrice) : IDomainEvent;
