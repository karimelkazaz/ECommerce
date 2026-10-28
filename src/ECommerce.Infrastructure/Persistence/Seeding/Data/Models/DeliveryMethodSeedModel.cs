using ECommerce.Domain.Entities.DeliveryMethods;

namespace ECommerce.Infrastructure.Persistence.Seeding.Data.Models;
public record DeliveryMethodSeedModel(Guid Id, string Name, string DeliveryTime, decimal Price, string CurrencyCode);
