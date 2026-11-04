using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Inventory;
using ECommerce.Domain.Entities.Products;

namespace ECommerce.Domain.Entities.Inventory.Events;

public sealed record InventoryItemCreatedDomainEvent(InventoryItemId InventoryItemId, ProductId ProductId) : IDomainEvent;
