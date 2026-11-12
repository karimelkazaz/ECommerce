namespace ECommerce.Infrastructure.Persistence.OrderExpiry;

public sealed class OrderExpiryOptions
{
    public const string SectionName = "OrderExpiry";

    public int PendingExpirationHours { get; set; } = 24;

    public int ScanIntervalMinutes { get; set; } = 30;
}
