namespace ECommerce.Infrastructure.Caching;

public interface ICacheAggregateStore<T> where T : class
{
    Task<T?> GetAsync(string key, CancellationToken cancellationToken = default);

    Task<T> GetOrCreateAsync(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken = default);

    Task SetAsync(string key, T value, CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
