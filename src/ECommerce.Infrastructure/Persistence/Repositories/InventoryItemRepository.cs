using ECommerce.Domain.Entities.Inventory;
using ECommerce.Domain.Entities.Products;
using ECommerce.Infrastructure.Persistence.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public sealed class InventoryItemRepository(ApplicationDbContext dbContext) : IInventoryItemRepository
{
    public Task<InventoryItem?> GetByProductIdAsync(ProductId productId, CancellationToken ct = default)
        => dbContext.InventoryItemsSet
            .Include(i => i.Ledger)
            .FirstOrDefaultAsync(i => i.ProductId == productId, ct);

    public Task<List<InventoryItem>> GetByProductIdsAsync(IReadOnlyCollection<ProductId> productIds, CancellationToken ct = default)
        => dbContext.InventoryItemsSet
            .Include(i => i.Ledger)
            .Where(i => productIds.Contains(i.ProductId))
            .ToListAsync(ct);

    public void Add(InventoryItem inventoryItem)
        => dbContext.InventoryItemsSet.Add(inventoryItem);
}
