using ECommerce.Domain.Entities.ProductsBrand;
using ECommerce.Infrastructure.Persistence.DbContexts;
using ECommerce.Infrastructure.Persistence.Seeding.Data.Models;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Persistence.Seeding;

public class ProductBrandSeeder(ApplicationDbContext dbContext, ILogger<ProductBrandSeeder> logger) : IDataSeeder
{
    public int Order => 1;

    public async Task SeedAsync(CancellationToken ct = default)
        => await JsonSeeder.SeedIfEmpty<ProductBrand, ProductBrandSeedModel>
                (dbContext.BrandsSet, "Brands.json",
                b => ProductBrand.Create(new ProductBrandId(b.Id), b.Name).Value, logger, ct);
}
