namespace ECommerce.Domain.Entities.DeliveryMethods;

public readonly record struct DeliveryMethodId(Guid Value)
{
    public static DeliveryMethodId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

