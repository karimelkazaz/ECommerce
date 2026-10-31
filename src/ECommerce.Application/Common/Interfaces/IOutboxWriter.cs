using MediatR;

namespace ECommerce.Application.Common.Interfaces;

public interface IOutboxWriter
{
    /// <summary>
    /// Enqueues a follow-up message in the current unit of work. The message is
    /// persisted atomically with the consumer's database changes when the
    /// surrounding transaction commits. Delivery remains at-least-once, so the
    /// eventual consumer must be idempotent.
    /// </summary>
    /// <remarks>
    /// Use <c>{outboxMessageId}:{consumerId}</c> from
    /// <see cref="IOutboxMessageContext.MessageId"/> as a stable idempotency key
    /// for the external service that ultimately handles the message.
    /// </remarks>
    void Enqueue(INotification message);
}
