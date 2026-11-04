using System.Linq.Expressions;
using ECommerce.Domain.Entities.Inventory;

namespace ECommerce.Application.Inventory;

public sealed record InventoryItemResponse(Guid ProductId, int AvailableQuantity)
{
    public static readonly Expression<Func<InventoryItem, InventoryItemResponse>> Projection = i => new InventoryItemResponse(
        i.ProductId.Value,
        i.AvailableQuantity);
}
