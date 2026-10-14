using Microsoft.Extensions.Caching.Hybrid;

namespace ECommerce.Infrastructure.Caching;

public static class HybridCacheExtensions
{
    private static readonly HybridCacheEntryOptions ReadOnlyOptions = new()
    {
        Flags = HybridCacheEntryFlags.DisableLocalCacheWrite | HybridCacheEntryFlags.DisableDistributedCacheWrite
    };

    public static async ValueTask<T?> TryGetAsync<T>(
        this HybridCache cache,
        string key, 
        CancellationToken cancellationToken = default)
        where T : class
    {
        var found = true;

        var value = await cache.GetOrCreateAsync(
            key,
            _ =>
            {
                found = false;
                return ValueTask.FromResult<T?>(default);
            },
            ReadOnlyOptions,
            cancellationToken: cancellationToken 
        );

        return found ? value : default;
    }
}
