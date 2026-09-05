namespace ECommerce.Infrastructure.Persistence.Otp;

public sealed class OtpCode
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Purpose { get; set; } = string.Empty;

    public string CodeHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedOnUtc { get; set; }

    public DateTimeOffset ExpiresOnUtc { get; set; }

    public int FailedAttempts { get; set; }

    public DateTimeOffset? VerifiedOnUtc { get; set; }

    public DateTimeOffset? InvalidatedOnUtc { get; set; }
}