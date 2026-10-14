using System.Text.Json;
using ECommerce.Domain.Entities.Baskets;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.ValueObjects;
using Xunit;

namespace ECommerce.Domain.UnitTests;

public class BasketTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly ProductId _productId = ProductId.New();
    private readonly Money _price = Money.Create(50m, Currency.Usd).Value;

    [Fact]
    public void Create_WithEmptyUserId_Fails()
    {
        var result = Basket.Create(Guid.Empty);

        Assert.True(result.IsFailure);
        Assert.Equal(BasketErrors.InvalidUserId.Code, result.Error.Code);
    }

    [Fact]
    public void Create_WithValidUserId_Succeeds()
    {
        var result = Basket.Create(_userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(_userId, result.Value.UserId);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalQuantity);
        Assert.Null(result.Value.SubTotal);
    }

    [Fact]
    public void AddItem_WhenNew_AddsItemToBasket()
    {
        var basket = Basket.Create(_userId).Value;

        var result = basket.AddItem(_productId, "Test Product", "http://image.png", _price, 2);

        Assert.True(result.IsSuccess);
        Assert.Single(basket.Items);
        Assert.Equal(2, basket.TotalQuantity);
        Assert.NotNull(basket.SubTotal);
        Assert.Equal(100m, basket.SubTotal.Amount);
        Assert.Equal(Currency.Usd, basket.SubTotal.Currency);
    }

    [Fact]
    public void AddItem_WhenExisting_IncreasesQuantityAndUpdatesPrice()
    {
        var basket = Basket.Create(_userId).Value;
        basket.AddItem(_productId, "Test Product", "http://image.png", _price, 2);

        var newPrice = Money.Create(60m, Currency.Usd).Value;
        var result = basket.AddItem(_productId, "Test Product", "http://image.png", newPrice, 3);

        Assert.True(result.IsSuccess);
        Assert.Single(basket.Items);
        Assert.Equal(5, basket.TotalQuantity);
        Assert.Equal(300m, basket.SubTotal!.Amount);
    }

    [Fact]
    public void AddItem_WithDifferentCurrency_Fails()
    {
        var basket = Basket.Create(_userId).Value;
        basket.AddItem(_productId, "Product 1", "http://image.png", _price, 1);

        var eurPrice = Money.Create(50m, Currency.Eur).Value;
        var result = basket.AddItem(ProductId.New(), "Product 2", "http://image.png", eurPrice, 1);

        Assert.True(result.IsFailure);
        Assert.Equal(BasketErrors.CurrencyMismatch.Code, result.Error.Code);
    }

    [Fact]
    public void RemoveItem_WhenPresent_RemovesItem()
    {
        var basket = Basket.Create(_userId).Value;
        basket.AddItem(_productId, "Test Product", "http://image.png", _price, 2);

        var result = basket.RemoveItem(_productId);

        Assert.True(result.IsSuccess);
        Assert.Empty(basket.Items);
        Assert.Equal(0, basket.TotalQuantity);
        Assert.Null(basket.SubTotal);
    }

    [Fact]
    public void RemoveItem_WhenNotFound_Fails()
    {
        var basket = Basket.Create(_userId).Value;

        var result = basket.RemoveItem(_productId);

        Assert.True(result.IsFailure);
        Assert.Equal(BasketErrors.ItemNotFound.Code, result.Error.Code);
    }

    [Fact]
    public void UpdateQuantity_WhenPresent_UpdatesQuantity()
    {
        var basket = Basket.Create(_userId).Value;
        basket.AddItem(_productId, "Test Product", "http://image.png", _price, 2);

        var result = basket.UpdateQuantity(_productId, 5);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, basket.TotalQuantity);
        Assert.Equal(250m, basket.SubTotal!.Amount);
    }

    [Fact]
    public void UpdateQuantity_WithZeroOrNegative_Fails()
    {
        var basket = Basket.Create(_userId).Value;
        basket.AddItem(_productId, "Test Product", "http://image.png", _price, 2);

        var result = basket.UpdateQuantity(_productId, 0);

        Assert.True(result.IsFailure);
        Assert.Equal(BasketErrors.InvalidQuantity.Code, result.Error.Code);
    }

    [Fact]
    public void MergeFrom_MergesItemsFromAnotherBasket()
    {
        var basket1 = Basket.Create(_userId).Value;
        basket1.AddItem(_productId, "Product 1", "http://img1.png", _price, 2);

        var otherUserId = Guid.NewGuid();
        var basket2 = Basket.Create(otherUserId).Value;
        var otherProduct = ProductId.New();
        basket2.AddItem(otherProduct, "Product 2", "http://img2.png", _price, 1);
        basket2.AddItem(_productId, "Product 1", "http://img1.png", _price, 3);

        var result = basket1.MergeFrom(basket2);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, basket1.Items.Count);
        Assert.Equal(6, basket1.TotalQuantity); // 2 + 3 of _productId, 1 of otherProduct
    }

    [Fact]
    public void MergeFrom_Self_Fails()
    {
        var basket = Basket.Create(_userId).Value;

        var result = basket.MergeFrom(basket);

        Assert.True(result.IsFailure);
        Assert.Equal(BasketErrors.CannotMergeSelf.Code, result.Error.Code);
    }

    [Fact]
    public void Clear_RemovesAllItems()
    {
        var basket = Basket.Create(_userId).Value;
        basket.AddItem(_productId, "Product 1", "http://img.png", _price, 2);

        basket.Clear();

        Assert.Empty(basket.Items);
        Assert.Equal(0, basket.TotalQuantity);
    }

    [Fact]
    public void JsonSerialization_RoundTrip_Succeeds()
    {
        var basket = Basket.Create(_userId).Value;
        basket.AddItem(_productId, "Product 1", "http://img.png", _price, 3);

        var json = JsonSerializer.Serialize(basket);
        var deserialized = JsonSerializer.Deserialize<Basket>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(basket.UserId, deserialized.UserId);
        Assert.Single(deserialized.Items);
        Assert.Equal(_productId, deserialized.Items[0].ProductId);
        Assert.Equal("Product 1", deserialized.Items[0].ProductName);
        Assert.Equal(3, deserialized.Items[0].Quantity);
        Assert.Equal(_price, deserialized.Items[0].Price);
        Assert.Equal(150m, deserialized.SubTotal!.Amount);
    }
}

