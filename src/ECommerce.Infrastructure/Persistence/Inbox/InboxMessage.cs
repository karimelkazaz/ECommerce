namespace ECommerce.Infrastructure.Persistence.Inbox;

public sealed class InboxMessage
{
    public Guid MessageId { get; private set; }
    public string Consumer { get; private set; } = null!;
    public string EventType { get; private set; } = null!;
    public DateTimeOffset ConsumedOnUtc { get; private set; }

    private InboxMessage() { }

    public static InboxMessage Claim(
        Guid messageId,
        string consumer,
        string eventType,
        DateTimeOffset consumedOnUtc)
        => new()
        {
            MessageId = messageId,
            Consumer = consumer,
            EventType = eventType,
            ConsumedOnUtc = consumedOnUtc,
        };
}
