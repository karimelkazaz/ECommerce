using ECommerce.Domain.Entities.ProductsType;
using ECommerce.Infrastructure.Persistence.DbContexts;
using ECommerce.Infrastructure.Persistence.Seeding.Data.Models;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Persistence.Seeding;

public class ProductTypeSeeder(ApplicationDbContext dbContext, ILogger<ProductTypeSeeder> logger) : IDataSeeder
{
    public int Order => 2;

    public async Task SeedAsync(CancellationToken ct = default)
        => await JsonSeeder.SeedIfEmpty<ProductType, ProductTypeSeedModel>
                (dbContext.TypesSet, "Types.json",
                b => ProductType.Create(new ProductTypeId(b.Id), b.Name).Value, logger, ct);
}
