namespace ECommerce.Application.Common.Interfaces;

public interface IInbox
{
    Task<bool> TryClaimAsync(
        Guid messageId,
        string eventType,
        string consumer,
        CancellationToken cancellationToken = default);
}
