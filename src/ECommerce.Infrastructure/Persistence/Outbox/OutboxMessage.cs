namespace ECommerce.Infrastructure.Persistence.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public DateTimeOffset OccurredOnUtc { get; private set; }
    public DateTimeOffset? ProcessedOnUtc { get; private set; }
    public int Attempts { get; private set; }
    public string? Error { get; private set; }
    public OutboxMessageStatus Status { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(string type, string content, DateTimeOffset occurredOnUtc)
        => new()
        {
            Id = Guid.NewGuid(),
            Type = type,
            Content = content,
            OccurredOnUtc = occurredOnUtc,
            Status = OutboxMessageStatus.Pending,
        };

    public void MarkProcessed(DateTimeOffset processedOnUtc)
    {
        ProcessedOnUtc = processedOnUtc;
        Error = null;
        Status = OutboxMessageStatus.Processed;
    }

    public void RecordFailure(string error, int maxAttempts)
    {
        Attempts += 1;
        Error = error.Length > 2000 ? error[..2000] : error;

        if (Attempts >= maxAttempts)
        {
            Status = OutboxMessageStatus.Failed;
        }
    }
}
