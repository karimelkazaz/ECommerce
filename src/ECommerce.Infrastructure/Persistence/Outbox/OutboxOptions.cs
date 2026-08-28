namespace ECommerce.Infrastructure.Persistence.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public int PollIntervalSeconds { get; set; } = 10;
    public int BatchSize { get; set; } = 50;
    public int MaxAttempts { get; set; } = 5;
}
