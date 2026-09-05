namespace ECommerce.Domain.Entities.RefreshTokens;

public readonly record struct RefreshTokenId(Guid Value)
{
    public static RefreshTokenId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}