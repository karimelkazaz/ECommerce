using ECommerce.Domain.Entities.DeliveryMethods;
using ECommerce.Infrastructure.Persistence.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public sealed class DeliveryMethodRepository(ApplicationDbContext dbContext) : IDeliveryMethodRepository
{
    public Task<DeliveryMethod?> GetByIdAsync(DeliveryMethodId id, CancellationToken ct = default)
        => dbContext.DeliveryMethodsSet.FirstOrDefaultAsync(d => d.Id == id, ct);

    public void Add(DeliveryMethod deliveryMethod)
        => dbContext.DeliveryMethodsSet.Add(deliveryMethod);

    public void Remove(DeliveryMethod deliveryMethod)
        => dbContext.DeliveryMethodsSet.Remove(deliveryMethod);
}
