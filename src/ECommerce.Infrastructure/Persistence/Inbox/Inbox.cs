using ECommerce.Application.Common.Interfaces;
using ECommerce.Infrastructure.Persistence.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Inbox;

public sealed class Inbox(ApplicationDbContext dbContext) : IInbox
{
    private Guid _messageId;
    private string _eventType = null!;

    public void BeginScope(Guid messageId, string eventType)
    {
        _messageId = messageId;
        _eventType = eventType;
    }

    public async Task<bool> TryClaimAsync(CancellationToken cancellationToken = default)
    {
        if (_messageId == Guid.Empty || _eventType is null)
        {
            throw new InvalidOperationException(
                "BeginScope must be called before TryClaimAsync.");
        }

        if (dbContext.InboxMessages.Local.Any(m => m.Id == _messageId))
        {
            return false;
        }

        var alreadyConsumed = await dbContext.InboxMessages
            .AnyAsync(m => m.Id == _messageId, cancellationToken);

        if (alreadyConsumed)
        {
            return false;
        }

        dbContext.InboxMessages.Add(
            InboxMessage.Claim(_messageId, _eventType, DateTimeOffset.UtcNow));

        return true;
    }
}
