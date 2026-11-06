using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Orders;
using ECommerce.Domain.Entities.Products;

namespace ECommerce.Domain.Entities.Inventory.Events;

public sealed record StockReservedDomainEvent(
    InventoryItemId InventoryItemId,
    ProductId ProductId,
    OrderId OrderId,
    int ReservedQuantity,
    int AvailableQuantity) : IDomainEvent;
