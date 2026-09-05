namespace ECommerce.Domain.Entities.UserAddresses;

public readonly record struct UserAddressId(Guid Value)
{
    public static UserAddressId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
