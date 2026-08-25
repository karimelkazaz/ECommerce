using ECommerce.Domain.Entities.Products;
using ECommerce.Infrastructure.Persistence.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository(ApplicationDbContext dbContext) : IProductRepository
{
    public Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default)
        => dbContext.ProductsSet.FirstOrDefaultAsync(p => p.Id == id, ct);

    public void Add(Product product)
        => dbContext.ProductsSet.Add(product);

    public void Remove(Product product)
        => dbContext.ProductsSet.Remove(product);
}
