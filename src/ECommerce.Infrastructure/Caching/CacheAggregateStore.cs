using ECommerce.Application.Common.Options;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Caching;

public sealed class CacheAggregateStore<T>(
    HybridCache cache,
    IOptionsMonitor<CacheEntryPolicy> options) :
    ICacheAggregateStore<T> where T : class
{
    private readonly CacheEntryPolicy _options = options.Get(typeof(T).Name);

    public async Task<T?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var envelope = await cache.TryGetAsync<CacheEnvelope<T>>(key, cancellationToken);
        if (envelope is null)
        {
            return null;
        }

        var isValid = await RefreshExpirationIfNeededAsync(
            key,
            envelope,
            cancellationToken);

        return isValid ? envelope.Payload : null;
    }

    public async Task<T> GetOrCreateAsync(string key, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken = default)
    {
        var envelope = await cache.GetOrCreateAsync(
            key,
            async ct =>
            {
                var value = await factory(ct);
                var utcNow = DateTime.UtcNow;
                return new CacheEnvelope<T> { Payload = value, CreatedAt = utcNow, LastAccessedUtc = utcNow };
            },
            CreateEntryOptionsForNewEnvelope(),
            cancellationToken: cancellationToken);

        await RefreshExpirationIfNeededAsync(key, envelope, cancellationToken);
        return envelope.Payload;
    }


    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await cache.RemoveAsync(key, cancellationToken);
    }

    public async Task SetAsync(string key, T value, CancellationToken cancellationToken = default)
    {
        var existing = await cache.TryGetAsync<CacheEnvelope<T>>(key, cancellationToken);

        var envelope = new CacheEnvelope<T>
        {
            Payload = value,
            CreatedAt = existing?.CreatedAt ?? DateTime.UtcNow,
            LastAccessedUtc = DateTime.UtcNow
        };

        await SetOrRemoveIfExpiredAsync(key, envelope, cancellationToken);
    }

    private async Task<bool> RefreshExpirationIfNeededAsync(
        string key,
        CacheEnvelope<T> envelope,
        CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        var expiration = CalculateExpiration(
            envelope.CreatedAt,
            envelope.LastAccessedUtc,
            utcNow);

        if (expiration is null)
        {
            await cache.RemoveAsync(key, cancellationToken);
            return false;
        }

        var age = utcNow - envelope.LastAccessedUtc;

        if (age < TimeSpan.FromMinutes(_options.SlidingRefreshMinutes))
        {
            return true;
        }

        var refreshed = new CacheEnvelope<T>
        {
            Payload = envelope.Payload,
            CreatedAt = envelope.CreatedAt,
            LastAccessedUtc = utcNow
        };

        expiration = CalculateExpiration(
            refreshed.CreatedAt,
            refreshed.LastAccessedUtc,
            utcNow);

        if (expiration is null)
        {
            await cache.RemoveAsync(key, cancellationToken);
            return false;
        }

        await cache.SetAsync(
            key,
            refreshed,
            CreateEntryOptions(expiration.Value),
            cancellationToken: cancellationToken);

        return true;
    }
    private async Task<bool> SetOrRemoveIfExpiredAsync(string key, CacheEnvelope<T> refreshed, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        var expiration = CalculateExpiration(
            refreshed.CreatedAt,
            refreshed.LastAccessedUtc,
            utcNow);

        if (expiration is null)
        {
            await cache.RemoveAsync(key, cancellationToken);
            return false;
        }

        var options = CreateEntryOptions(expiration.Value);

        await cache.SetAsync(key, refreshed, options, cancellationToken: cancellationToken);
        return true;
    }

    private HybridCacheEntryOptions CreateEntryOptionsForNewEnvelope()
    {
        var utcNow = DateTime.UtcNow;

        var expiration = CalculateExpiration(utcNow, utcNow, utcNow) ?? throw new InvalidOperationException("Cannot create a cache entry with no expiration.");
        return CreateEntryOptions(expiration);
    }

    private TimeSpan? CalculateExpiration(
        DateTime createdAtUtc,
        DateTime lastAccessedUtc,
        DateTime utcNow)
    {
        var absoluteRemaining = createdAtUtc
            .AddDays(_options.AbsoluteExpirationDays)
            .Subtract(utcNow);

        var slidingRemaining = lastAccessedUtc
            .AddDays(_options.SlidingExpirationDays)
            .Subtract(utcNow);

        if (absoluteRemaining <= TimeSpan.Zero || slidingRemaining <= TimeSpan.Zero)
        {
            return null;
        }

        return absoluteRemaining <= slidingRemaining ? absoluteRemaining : slidingRemaining;
    }

    private HybridCacheEntryOptions CreateEntryOptions(TimeSpan expiration)
    {
        var localCacheExpiration = TimeSpan.FromMinutes(_options.LocalCacheExpirationMinutes);

        if (localCacheExpiration >= expiration)
        {
            localCacheExpiration = expiration;
        }

        return new HybridCacheEntryOptions
        {
            Expiration = expiration,
            LocalCacheExpiration = localCacheExpiration
        };
    }
}
