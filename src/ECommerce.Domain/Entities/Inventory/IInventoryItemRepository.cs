using ECommerce.Domain.Entities.Products;

namespace ECommerce.Domain.Entities.Inventory
{
    public interface IInventoryItemRepository
    {
        Task<InventoryItem?> GetByProductIdAsync(ProductId productId, CancellationToken ct = default);

        Task<List<InventoryItem>> GetByProductIdsAsync(IReadOnlyCollection<ProductId> productIds, CancellationToken ct = default);

        void Add(InventoryItem inventoryItem);
    }
}
