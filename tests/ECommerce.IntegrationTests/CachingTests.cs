using ECommerce.Application.Common.Options;
using ECommerce.Domain.Entities.Baskets;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.Repositories;
using ECommerce.Domain.ValueObjects;
using ECommerce.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace ECommerce.IntegrationTests;

public class CacheEntryPolicyValidatorTests
{
    private readonly CacheEntryPolicyValidator _validator = new();

    [Fact]
    public void Validate_WithDefaultOptions_Succeeds()
    {
        var options = new CacheEntryPolicy();

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithInvalidAbsoluteExpiration_Fails(int days)
    {
        var options = new CacheEntryPolicy { AbsoluteExpirationDays = days };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f => f.Contains("Absolute expiration"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithInvalidSlidingExpiration_Fails(int days)
    {
        var options = new CacheEntryPolicy { SlidingExpirationDays = days };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f => f.Contains("Sliding expiration"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithInvalidLocalCacheExpiration_Fails(int minutes)
    {
        var options = new CacheEntryPolicy { LocalCacheExpirationMinutes = minutes };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f => f.Contains("Local cache expiration"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithInvalidSlidingRefresh_Fails(int minutes)
    {
        var options = new CacheEntryPolicy { SlidingRefreshMinutes = minutes };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f => f.Contains("Sliding refresh"));
    }

    [Fact]
    public void Validate_WhenSlidingGreaterThanAbsolute_Fails()
    {
        var options = new CacheEntryPolicy
        {
            AbsoluteExpirationDays = 5,
            SlidingExpirationDays = 10
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f => f.Contains("cannot be greater than absolute"));
    }

    [Fact]
    public void Validate_WithNullOptions_Fails()
    {
        var result = _validator.Validate(null, null!);

        Assert.True(result.Failed);
    }
}

public class BasketStoreCachingTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IBasketStore _basketStore;
    private readonly ICacheAggregateStore<Basket> _aggregateStore;

    public BasketStoreCachingTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHybridCache();

        services.AddOptions<CacheEntryPolicy>("Basket")
            .Configure(options =>
            {
                options.AbsoluteExpirationDays = 30;
                options.SlidingExpirationDays = 7;
                options.LocalCacheExpirationMinutes = 5;
                options.SlidingRefreshMinutes = 30;
            });

        services.AddSingleton<IValidateOptions<CacheEntryPolicy>, CacheEntryPolicyValidator>();
        services.AddScoped(typeof(ICacheAggregateStore<>), typeof(CacheAggregateStore<>));
        services.AddScoped<IBasketStore, BasketStore>();

        _serviceProvider = services.BuildServiceProvider();
        var scope = _serviceProvider.CreateScope();
        _basketStore = scope.ServiceProvider.GetRequiredService<IBasketStore>();
        _aggregateStore = scope.ServiceProvider.GetRequiredService<ICacheAggregateStore<Basket>>();
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenNotExists_CreatesAndReturnsNewBasket()
    {
        var userId = Guid.NewGuid();

        var basket = await _basketStore.GetOrCreateAsync(userId);

        Assert.NotNull(basket);
        Assert.Equal(userId, basket.UserId);
        Assert.Empty(basket.Items);
    }

    [Fact]
    public async Task GetOrCreateAsync_WithEmptyGuid_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _basketStore.GetOrCreateAsync(Guid.Empty));
    }

    [Fact]
    public async Task GetAsync_WhenNotExists_ReturnsNull()
    {
        var userId = Guid.NewGuid();

        var basket = await _basketStore.GetAsync(userId);

        Assert.Null(basket);
    }

    [Fact]
    public async Task SaveAsync_ThenGetAsync_ReturnsSavedBasket()
    {
        var userId = Guid.NewGuid();
        var basket = await _basketStore.GetOrCreateAsync(userId);
        var productId = ProductId.New();
        var price = Money.Create(25m, Currency.Usd).Value;

        basket.AddItem(productId, "Product 1", "http://img.png", price, 2);
        await _basketStore.SaveAsync(basket);

        var retrieved = await _basketStore.GetAsync(userId);

        Assert.NotNull(retrieved);
        Assert.Equal(userId, retrieved.UserId);
        Assert.Single(retrieved.Items);
        Assert.Equal(2, retrieved.TotalQuantity);
        Assert.Equal(50m, retrieved.SubTotal!.Amount);
    }

    [Fact]
    public async Task DeleteAsync_RemovesBasketFromCache()
    {
        var userId = Guid.NewGuid();
        var basket = await _basketStore.GetOrCreateAsync(userId);
        await _basketStore.SaveAsync(basket);

        await _basketStore.DeleteAsync(userId);

        var retrieved = await _basketStore.GetAsync(userId);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task CacheAggregateStore_DirectGetAndSet_Works()
    {
        var userId = Guid.NewGuid();
        var basket = Basket.Create(userId).Value;
        var key = $"basket:{userId}";

        await _aggregateStore.SetAsync(key, basket);

        var retrieved = await _aggregateStore.GetAsync(key);
        Assert.NotNull(retrieved);
        Assert.Equal(userId, retrieved.UserId);

        await _aggregateStore.RemoveAsync(key);
        var afterRemove = await _aggregateStore.GetAsync(key);
        Assert.Null(afterRemove);
    }
}

