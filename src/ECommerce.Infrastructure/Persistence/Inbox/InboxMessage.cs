namespace ECommerce.Infrastructure.Persistence.Inbox;

public sealed class InboxMessage
{
    public Guid Id { get; private set; }
    public string EventType { get; private set; } = null!;
    public DateTimeOffset ConsumedOnUtc { get; private set; }

    private InboxMessage() { }

    public static InboxMessage Claim(Guid id, string eventType, DateTimeOffset consumedOnUtc)
        => new()
        {
            Id = id,
            EventType = eventType,
            ConsumedOnUtc = consumedOnUtc,
        };
}
