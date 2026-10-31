using ECommerce.Application.Common.Interfaces;
using ECommerce.Infrastructure.Persistence.DbContexts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Inbox;

public sealed class Inbox(ApplicationDbContext dbContext, TimeProvider timeProvider) : IInbox
{
    public async Task<bool> TryClaimAsync(
        Guid messageId,
        string eventType,
        string consumer,
        CancellationToken cancellationToken = default)
    {
        if (messageId == Guid.Empty)
        {
            throw new ArgumentException("A message ID is required.", nameof(messageId));
        }

        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException("An event type is required.", nameof(eventType));
        }

        if (string.IsNullOrWhiteSpace(consumer))
        {
            throw new ArgumentException("A consumer identifier is required.", nameof(consumer));
        }

        if (dbContext.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "Inbox claims must be written inside the consumer transaction.");
        }

        var consumedOnUtc = timeProvider.GetUtcNow();

        try
        {
            var inserted = await dbContext.Database.ExecuteSqlAsync(
                $"""
                INSERT INTO InboxMessages (MessageId, Consumer, EventType, ConsumedOnUtc)
                SELECT {messageId}, {consumer}, {eventType}, {consumedOnUtc}
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM InboxMessages WITH (UPDLOCK, HOLDLOCK)
                    WHERE MessageId = {messageId} AND Consumer = {consumer}
                );
                """,
                cancellationToken);

            return inserted > 0;
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            // A concurrent transaction inserted the same claim first.
            return false;
        }
    }
}
