namespace ECommerce.Infrastructure.Persistence.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "ECommerce.Api";

    public string Audience { get; init; } = "ECommerce.Clients";

    public string SecretKey { get; init; } = string.Empty;

    public int ExpirationMinutes { get; init; } = 120;

    public int RefreshTokenExpirationDays { get; init; } = 30;
}