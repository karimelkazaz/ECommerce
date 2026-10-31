using ECommerce.Application.Common.Interfaces;

namespace ECommerce.Infrastructure.Persistence.Outbox;

public sealed class OutboxMessageContext : IOutboxMessageContext
{
    private Guid? _messageId;
    private string? _eventType;

    public Guid MessageId => _messageId ?? throw NotAvailable();

    public string EventType => _eventType ?? throw NotAvailable();

    public bool IsAvailable => _messageId is not null && _eventType is not null;

    /// <summary>
    /// Initializes the context for one outbox message. Called once by the
    /// outbox processor in a fresh message scope.
    /// </summary>
    public void Initialize(Guid messageId, string eventType)
    {
        if (messageId == Guid.Empty)
        {
            throw new ArgumentException("A message ID is required.", nameof(messageId));
        }

        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException("An event type is required.", nameof(eventType));
        }

        if (IsAvailable)
        {
            throw new InvalidOperationException(
                "The outbox message context has already been initialized for this scope.");
        }

        _messageId = messageId;
        _eventType = eventType;
    }

    private static InvalidOperationException NotAvailable()
        => new("The outbox message context is only available while the outbox processor is publishing a message.");
}
