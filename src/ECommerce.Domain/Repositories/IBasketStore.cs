using ECommerce.Domain.Entities.Baskets;

namespace ECommerce.Domain.Repositories;

public interface IBasketStore
{
    Task<Basket?> GetAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Basket> GetOrCreateAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SaveAsync(Basket basket, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default);
}