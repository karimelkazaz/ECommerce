using System.Text.Json.Serialization;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities.Baskets;

public class BasketItem
{
    public ProductId ProductId { get; private set; }
    public string ProductName { get; private set; } = null!;
    public string PictureUrl { get; private set; } = null!;
    public Money Price { get; private set; } = null!;
    public int Quantity { get; private set; }

    private BasketItem() { }

    [JsonConstructor]
    internal BasketItem(
        ProductId productId,
        string productName,
        string pictureUrl,
        Money price,
        int quantity)
    {
        ProductId = productId;
        ProductName = productName;
        PictureUrl = pictureUrl;
        Price = price;
        Quantity = quantity;
    }

    internal static Result<BasketItem> Create(
        ProductId productId,
        string productName,
        string pictureUrl,
        Money price,
        int quantity)
    {
        if (productId == default)
            return Result.Failure<BasketItem>(BasketErrors.InvalidProductId);

        if (string.IsNullOrWhiteSpace(productName))
            return Result.Failure<BasketItem>(BasketErrors.ProductNameRequired);

        if (string.IsNullOrWhiteSpace(pictureUrl))
            return Result.Failure<BasketItem>(BasketErrors.PictureUrlRequired);

        if (price is null)
            return Result.Failure<BasketItem>(BasketErrors.PriceRequired);

        if (quantity < 1)
            return Result.Failure<BasketItem>(BasketErrors.InvalidQuantity);

        return Result.Success(new BasketItem
        {
            ProductId = productId,
            ProductName = productName,
            PictureUrl = pictureUrl,
            Price = price,
            Quantity = quantity
        });
    }

    public decimal LineTotal => Price.Amount * Quantity;

    internal Result ChangeQuantity(int quantity)
    {
        if (quantity < 1)
            return Result.Failure(BasketErrors.InvalidQuantity);

        Quantity = quantity;
        return Result.Success();
    }

    internal Result IncreaseQuantity(int amount)
    {
        if (amount < 1)
            return Result.Failure(BasketErrors.InvalidQuantity);

        Quantity += amount;
        return Result.Success();
    }

    internal Result UpdatePrice(Money price)
    {
        if (price is null)
            return Result.Failure(BasketErrors.PriceRequired);

        if (Price.Currency != price.Currency)
            return Result.Failure(BasketErrors.CurrencyMismatch);

        Price = price;
        return Result.Success();
    }
}
