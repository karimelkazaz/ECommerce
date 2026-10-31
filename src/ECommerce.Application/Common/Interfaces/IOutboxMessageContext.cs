namespace ECommerce.Application.Common.Interfaces;

/// <summary>
/// Read-only information about the outbox message currently being published.
/// Consumers use <see cref="MessageId"/> to build stable idempotency keys of the
/// form <c>{outboxMessageId}:{consumerId}</c> for external side effects.
/// </summary>
public interface IOutboxMessageContext
{
    Guid MessageId { get; }

    string EventType { get; }

    bool IsAvailable { get; }
}
