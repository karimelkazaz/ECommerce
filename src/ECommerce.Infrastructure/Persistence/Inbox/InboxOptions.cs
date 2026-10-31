namespace ECommerce.Infrastructure.Persistence.Inbox;

public sealed class InboxOptions
{
    public const string SectionName = "Inbox";

    public int RetentionDays { get; set; } = 30;
    public int CleanupIntervalMinutes { get; set; } = 60;
}
