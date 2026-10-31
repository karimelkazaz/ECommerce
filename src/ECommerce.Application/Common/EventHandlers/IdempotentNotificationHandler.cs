using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Common.EventHandlers;

/// <summary>
/// Handles an outbox notification exactly once per consumer in the database.
/// The inbox claim and the consumer's database changes commit or roll back
/// together. External side effects must not be performed inline; enqueue a
/// follow-up outbox message through <see cref="IOutboxWriter"/> instead.
/// </summary>
public abstract class IdempotentNotificationHandler<TNotification>(
    IInbox inbox,
    IUnitOfWork unitOfWork,
    IOutboxMessageContext messageContext,
    ILogger logger)
    : INotificationHandler<TNotification>
    where TNotification : INotification
{
    /// <summary>
    /// Stable, explicit consumer identifier. Never derive this from a type name
    /// that can change when files move. Append a version suffix (for example
    /// <c>:v2</c>) when a consumer's logic must replay old messages.
    /// </summary>
    protected abstract string ConsumerId { get; }

    public Task Handle(TNotification notification, CancellationToken cancellationToken)
        => unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                if (!messageContext.IsAvailable)
                {
                    throw new InvalidOperationException(
                        $"{GetType().Name} can only handle notifications published by the outbox processor.");
                }

                if (!await inbox.TryClaimAsync(
                        messageContext.MessageId,
                        messageContext.EventType,
                        ConsumerId,
                        ct))
                {
                    logger.LogDebug(
                        "Suppressed duplicate delivery of {EventType} ({MessageId}) for consumer {Consumer}.",
                        messageContext.EventType,
                        messageContext.MessageId,
                        ConsumerId);
                    return;
                }

                try
                {
                    await HandleCore(notification, ct);
                    await unitOfWork.SaveChangesAsync(ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Consumer {Consumer} failed handling {EventType} ({MessageId}).",
                        ConsumerId,
                        messageContext.EventType,
                        messageContext.MessageId);
                    throw;
                }
            },
            cancellationToken);

    protected abstract Task HandleCore(
        TNotification notification,
        CancellationToken cancellationToken);
}
