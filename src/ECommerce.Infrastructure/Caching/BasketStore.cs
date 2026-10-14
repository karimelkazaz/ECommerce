using ECommerce.Domain.Entities.Baskets;
using ECommerce.Domain.Repositories;

namespace ECommerce.Infrastructure.Caching;

public sealed class BasketStore(ICacheAggregateStore<Basket> store) : IBasketStore
{
    public Task<Basket?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
        => store.GetAsync(GetKey(userId), cancellationToken);

    public Task<Basket> GetOrCreateAsync(Guid userId, CancellationToken cancellationToken = default)
        => store.GetOrCreateAsync(
            GetKey(userId),
            _ =>
            {
                var basketResult = Basket.Create(userId);
                if (basketResult.IsFailure)
                {
                    throw new InvalidOperationException($"Failed to create basket for user '{userId}': {basketResult.Error.Description}");
                }

                return Task.FromResult(basketResult.Value);
            },
            cancellationToken: cancellationToken
        );

    public Task SaveAsync(Basket basket, CancellationToken cancellationToken = default)
        => store.SetAsync(GetKey(basket.UserId), basket, cancellationToken);

    public Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default)
        => store.RemoveAsync(GetKey(userId), cancellationToken);

    private static string GetKey(Guid userId) => $"basket:{userId}";
}
