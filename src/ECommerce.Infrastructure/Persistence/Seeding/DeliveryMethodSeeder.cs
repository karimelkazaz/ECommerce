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
    {
        DeliveryMethod Map(DeliveryMethodSeedModel d)
        {
            var deliveryMethod = DeliveryMethod.Create(
                    new DeliveryMethodId(d.Id),
                    d.Name,
                    d.DeliveryTime,
                    Money.Create(d.Price, Currency.FromCode(d.CurrencyCode)).Value,
                    d.Description).Value;

            if (!d.IsAvailable)
                deliveryMethod.ChangeAvailability(false);

            return deliveryMethod;
        }

        await JsonSeeder.SeedIfEmpty<DeliveryMethod, DeliveryMethodSeedModel>
                (dbContext.DeliveryMethodsSet, "DeliveryMethods.json",
                Map,
                logger, ct);
    }
}
