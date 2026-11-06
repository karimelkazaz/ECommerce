using MediatR;

namespace ECommerce.Application.Inventory;

/// <summary>
/// Integration message emitted when a product's availability reaches zero and
/// is still zero at consumption time. Enqueued by the reserved/adjusted stock
/// consumers (release frees stock, so it never depletes) and consumed by
/// <see cref="EventHandlers.StockDepletedNotificationHandler"/> after the
/// originating transaction committed; follow-up consumers (restock alerts,
/// merchandising) delegate exhaustion-sensitive side effects here.
/// </summary>
public sealed record StockDepletedNotification(Guid InventoryItemId, Guid ProductId) : INotification;
