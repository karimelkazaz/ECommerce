using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.DeliveryMethods;

namespace ECommerce.Domain.ValueObjects;

public sealed class OrderDeliveryMethod : ValueObject
{
    public string Name { get; private set; } = null!;
    public string DeliveryTime { get; private set; } = null!;

    public Money Price { get; private set; } = null!;

    private OrderDeliveryMethod() { }

    private OrderDeliveryMethod(string name, string deliveryTime, Money price)
    {
        Name = name;
        DeliveryTime = deliveryTime;
        Price = price;
    }

    public static OrderDeliveryMethod FromEntity(DeliveryMethod method)
    {
        return new OrderDeliveryMethod(
            method.Name,
            method.DeliveryTime,
            method.Price);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Name;
        yield return DeliveryTime;
        yield return Price;
    }
}

