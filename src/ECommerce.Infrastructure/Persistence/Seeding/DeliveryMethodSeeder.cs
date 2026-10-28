using ECommerce.Domain.Entities.DeliveryMethods;
using ECommerce.Domain.ValueObjects;
using ECommerce.Infrastructure.Persistence.DbContexts;
using ECommerce.Infrastructure.Persistence.Seeding.Data.Models;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Persistence.Seeding;

public class DeliveryMethodSeeder(ApplicationDbContext dbContext, ILogger<DeliveryMethodSeeder> logger) : IDataSeeder
{
    public int Order => 3;

    public async Task SeedAsync(CancellationToken ct = default)
        => await JsonSeeder.SeedIfEmpty<DeliveryMethod, DeliveryMethodSeedModel>
                (dbContext.DeliveryMethodsSet, "DeliveryMethods.json",
                d => DeliveryMethod.Create(
                        new DeliveryMethodId(d.Id),
                        d.Name,
                        d.DeliveryTime,
                        Money.Create(d.Price, Currency.FromCode(d.CurrencyCode)).Value).Value,
                logger, ct);
}
