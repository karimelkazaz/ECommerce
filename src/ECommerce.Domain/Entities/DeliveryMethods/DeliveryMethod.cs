using ECommerce.Domain.Common;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities.DeliveryMethods;

public class DeliveryMethod : Entity<DeliveryMethodId>
{
    public string Name { get; private set; } = null!;
    public string DeliveryTime { get; private set; } = null!;
    public Money Price { get; private set; } = null!;

    private DeliveryMethod() { }

    private DeliveryMethod(
        DeliveryMethodId id,
        string name,
        string deliveryTime,
        Money price) : base(id)
    {
        Name = name;
        DeliveryTime = deliveryTime;
        Price = price;
    }

    public static Result<DeliveryMethod> Create(
        DeliveryMethodId id,
        string name,
        string deliveryTime,
        Money price)
    {
        if (id == default)
            return Result.Failure<DeliveryMethod>(DeliveryMethodErrors.InvalidId);

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<DeliveryMethod>(DeliveryMethodErrors.NameRequired);

        if (price is null)
            return Result.Failure<DeliveryMethod>(DeliveryMethodErrors.PriceRequired);

        return Result.Success(new DeliveryMethod(id, name, deliveryTime, price));
    }
}

