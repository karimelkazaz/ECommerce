using ECommerce.Domain.Entities.ProductsBrand;
using ECommerce.Infrastructure.Persistence.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public sealed class ProductBrandRepository(ApplicationDbContext dbContext) : IProductBrandRepository
{
    public Task<ProductBrand?> GetByIdAsync(ProductBrandId id, CancellationToken ct = default)
        => dbContext.BrandsSet.FirstOrDefaultAsync(b => b.Id == id, ct);

    public void Add(ProductBrand brand)
        => dbContext.BrandsSet.Add(brand);

    public void Remove(ProductBrand brand)
        => dbContext.BrandsSet.Remove(brand);
}
