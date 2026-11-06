using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Products;

namespace ECommerce.Domain.Entities.Inventory.Events;

public sealed record StockAdjustedDomainEvent(
    InventoryItemId InventoryItemId,
    ProductId ProductId,
    int AdjustedDelta,
    int AvailableQuantity) : IDomainEvent;
