using ECommerce.Domain.Entities.ProductsType;
using ECommerce.Infrastructure.Persistence.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public sealed class ProductTypeRepository(ApplicationDbContext dbContext) : IProductTypeRepository
{
    public Task<ProductType?> GetByIdAsync(ProductTypeId id, CancellationToken ct = default)
        => dbContext.TypesSet.FirstOrDefaultAsync(t => t.Id == id, ct);

    public void Add(ProductType type)
        => dbContext.TypesSet.Add(type);

    public void Remove(ProductType type)
        => dbContext.TypesSet.Remove(type);
}
