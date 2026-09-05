namespace ECommerce.Infrastructure.Persistence.Otp;

public sealed class OtpOptions
{
    public const string SectionName = "Otp";

    public int CodeLength { get; init; } = 6;

    public int ExpirationMinutes { get; init; } = 5;

    public int MaxFailedAttempts { get; init; } = 5;
}