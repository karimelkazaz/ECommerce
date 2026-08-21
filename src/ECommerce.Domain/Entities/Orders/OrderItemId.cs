namespace ECommerce.Domain.Entities.Orders;

public readonly record struct OrderItemId(Guid Value)
{
    public static OrderItemId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

