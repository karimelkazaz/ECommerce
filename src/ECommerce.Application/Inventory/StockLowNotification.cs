using MediatR;

namespace ECommerce.Application.Inventory;

/// <summary>
/// Integration message emitted when a product's availability falls to or below
/// the configured low-stock threshold while still above zero. Enqueued by the
/// reserved/adjusted stock consumers through <c>StockAttentionFollowUps</c> and
/// consumed by
/// <see cref="EventHandlers.StockLowNotificationHandler"/> after the
/// originating transaction committed.
/// </summary>
public sealed record StockLowNotification(Guid InventoryItemId, Guid ProductId, int AvailableQuantity) : INotification;
