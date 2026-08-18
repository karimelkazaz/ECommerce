using ECommerce.Domain.Entities.ProductsBrand;
using ECommerce.Infrastructure.Persistence.DbContexts;
using ECommerce.Infrastructure.Persistence.Seeding.Data.Models;

namespace ECommerce.Infrastructure.Persistence.Seeding;

public class ProductBrandSeeder(ApplicationDbContext dbContext) : IDataSeeder
{
    public int Order => 1;

    public async Task SeedAsync(CancellationToken ct = default)
        => await JsonSeeder.SeedIfEmpty<ProductBrand, ProductBrandSeedModel>
                (dbContext.BrandsSet, "brands.json", b => ProductBrand.Create(new ProductBrandId(b.Id), b.Name).Value, ct);
}