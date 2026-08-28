namespace ECommerce.Application.Common.Interfaces;

public interface IInbox
{
    void BeginScope(Guid messageId, string eventType);

    Task<bool> TryClaimAsync(CancellationToken cancellationToken = default);
}
