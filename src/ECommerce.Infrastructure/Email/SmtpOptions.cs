namespace ECommerce.Infrastructure.Email;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; init; } = string.Empty;

    public int Port { get; init; } = 587;

    public string FromEmail { get; init; } = string.Empty;

    public string FromName { get; init; } = "ECommerce";

    public string? Username { get; init; }

    public string? Password { get; init; }

    public bool EnableSsl { get; init; } = true;
}