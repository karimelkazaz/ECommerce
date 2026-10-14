using ECommerce.Application.Baskets;
using ECommerce.Application.Baskets.Commands;
using ECommerce.Application.Baskets.Queries;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Baskets;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.Repositories;
using ECommerce.Domain.ValueObjects;
using Xunit;

namespace ECommerce.IntegrationTests;

public class BasketApplicationTests
{
    private readonly FakeBasketStore _basketStore = new();
    private readonly FakeProductRepository _productRepository = new();
    private readonly FakeCurrentUserService _currentUserService = new();

    public BasketApplicationTests()
    {
        _currentUserService.UserId = Guid.NewGuid();
    }

    [Fact]
    public async Task GetBasketQueryHandler_WhenCalled_ReturnsUserBasket()
    {
        var handler = new GetBasketQueryHandler(_basketStore, _currentUserService);

        var result = await handler.Handle(new GetBasketQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(_currentUserService.UserId, result.Value.UserId);
        Assert.Empty(result.Value.Items);
    }

    [Fact]
    public async Task GetBasketQueryHandler_WithoutUserId_Fails()
    {
        _currentUserService.UserId = null;
        var handler = new GetBasketQueryHandler(_basketStore, _currentUserService);

        var result = await handler.Handle(new GetBasketQuery(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(BasketErrors.InvalidUserId.Code, result.Error.Code);
    }

    [Fact]
    public async Task AddItemToBasketCommandHandler_WithValidProduct_AddsItemAndReturnsBasket()
    {
        var productId = ProductId.New();
        var product = Product.Create(productId, "Product 1", "Description", "http://img.png", Money.Create(20m, Currency.Usd).Value).Value;
        _productRepository.Add(product);

        var handler = new AddItemToBasketCommandHandler(_basketStore, _productRepository, _currentUserService);

        var result = await handler.Handle(new AddItemToBasketCommand(productId.Value, 2), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal(2, result.Value.TotalQuantity);
        Assert.Equal(40m, result.Value.SubTotalAmount);
    }

    [Fact]
    public async Task AddItemToBasketCommandHandler_ProductNotFound_Fails()
    {
        var handler = new AddItemToBasketCommandHandler(_basketStore, _productRepository, _currentUserService);

        var result = await handler.Handle(new AddItemToBasketCommand(Guid.NewGuid(), 1), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task UpdateBasketItemQuantityCommandHandler_UpdatesQuantity()
    {
        var productId = ProductId.New();
        var basket = await _basketStore.GetOrCreateAsync(_currentUserService.UserId!.Value);
        basket.AddItem(productId, "Product 1", "http://img.png", Money.Create(10m, Currency.Usd).Value, 1);
        await _basketStore.SaveAsync(basket);

        var handler = new UpdateBasketItemQuantityCommandHandler(_basketStore, _currentUserService);

        var result = await handler.Handle(new UpdateBasketItemQuantityCommand(productId.Value, 5), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.TotalQuantity);
        Assert.Equal(50m, result.Value.SubTotalAmount);
    }

    [Fact]
    public async Task RemoveBasketItemCommandHandler_RemovesItem()
    {
        var productId = ProductId.New();
        var basket = await _basketStore.GetOrCreateAsync(_currentUserService.UserId!.Value);
        basket.AddItem(productId, "Product 1", "http://img.png", Money.Create(10m, Currency.Usd).Value, 1);
        await _basketStore.SaveAsync(basket);

        var handler = new RemoveBasketItemCommandHandler(_basketStore, _currentUserService);

        var result = await handler.Handle(new RemoveBasketItemCommand(productId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
    }

    [Fact]
    public async Task ClearBasketCommandHandler_DeletesBasket()
    {
        var basket = await _basketStore.GetOrCreateAsync(_currentUserService.UserId!.Value);
        basket.AddItem(ProductId.New(), "Product 1", "http://img.png", Money.Create(10m, Currency.Usd).Value, 1);
        await _basketStore.SaveAsync(basket);

        var handler = new ClearBasketCommandHandler(_basketStore, _currentUserService);

        var result = await handler.Handle(new ClearBasketCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var inStore = await _basketStore.GetAsync(_currentUserService.UserId!.Value);
        Assert.Null(inStore);
    }

    [Fact]
    public async Task MergeBasketCommandHandler_MergesBaskets()
    {
        var guestUserId = Guid.NewGuid();
        var guestBasket = await _basketStore.GetOrCreateAsync(guestUserId);
        var product1 = ProductId.New();
        guestBasket.AddItem(product1, "P1", "http://img.png", Money.Create(15m, Currency.Usd).Value, 2);
        await _basketStore.SaveAsync(guestBasket);

        var handler = new MergeBasketCommandHandler(_basketStore, _currentUserService);

        var result = await handler.Handle(new MergeBasketCommand(guestUserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal(2, result.Value.TotalQuantity);

        // Guest basket should be deleted
        var deletedGuestBasket = await _basketStore.GetAsync(guestUserId);
        Assert.Null(deletedGuestBasket);
    }

    [Fact]
    public void AddItemToBasketCommandValidator_ValidatesProductIdAndQuantity()
    {
        var validator = new AddItemToBasketCommandValidator();

        var invalidProduct = validator.Validate(new AddItemToBasketCommand(Guid.Empty, 1));
        Assert.False(invalidProduct.IsValid);

        var invalidQuantity = validator.Validate(new AddItemToBasketCommand(Guid.NewGuid(), 0));
        Assert.False(invalidQuantity.IsValid);

        var valid = validator.Validate(new AddItemToBasketCommand(Guid.NewGuid(), 2));
        Assert.True(valid.IsValid);
    }

    [Fact]
    public void UpdateBasketItemQuantityCommandValidator_ValidatesQuantity()
    {
        var validator = new UpdateBasketItemQuantityCommandValidator();

        var invalid = validator.Validate(new UpdateBasketItemQuantityCommand(Guid.NewGuid(), -1));
        Assert.False(invalid.IsValid);

        var valid = validator.Validate(new UpdateBasketItemQuantityCommand(Guid.NewGuid(), 3));
        Assert.True(valid.IsValid);
    }

    [Fact]
    public void MergeBasketCommandValidator_ValidatesSourceUserId()
    {
        var validator = new MergeBasketCommandValidator();

        var invalid = validator.Validate(new MergeBasketCommand(Guid.Empty));
        Assert.False(invalid.IsValid);

        var valid = validator.Validate(new MergeBasketCommand(Guid.NewGuid()));
        Assert.True(valid.IsValid);
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? UserId { get; set; }
        public string? Email { get; set; }
        public bool IsAuthenticated => UserId.HasValue;
    }

    private sealed class FakeBasketStore : IBasketStore
    {
        private readonly Dictionary<Guid, Basket> _baskets = new();

        public Task<Basket?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            _baskets.TryGetValue(userId, out var basket);
            return Task.FromResult(basket);
        }

        public Task<Basket> GetOrCreateAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            if (!_baskets.TryGetValue(userId, out var basket))
            {
                basket = Basket.Create(userId).Value;
                _baskets[userId] = basket;
            }
            return Task.FromResult(basket);
        }

        public Task SaveAsync(Basket basket, CancellationToken cancellationToken = default)
        {
            _baskets[basket.UserId] = basket;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            _baskets.Remove(userId);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        private readonly Dictionary<ProductId, Product> _products = new();

        public Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default)
        {
            _products.TryGetValue(id, out var product);
            return Task.FromResult(product);
        }

        public void Add(Product product) => _products[product.Id] = product;
        public void Remove(Product product) => _products.Remove(product.Id);
    }
}

