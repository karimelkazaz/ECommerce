using System.Text.Json.Serialization;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities.Baskets;

public class Basket
{
    private List<BasketItem> _items = [];

    public Guid UserId { get; private set; }

    public List<BasketItem> Items => _items;
    public int TotalQuantity => _items.Sum(i => i.Quantity);

    public Money? SubTotal
    {
        get
        {
            if (_items.Count == 0)
                return null;

            var currency = _items[0].Price.Currency;
            decimal amount = _items.Sum(i => i.LineTotal);
            return Money.Create(amount, currency).Value;
        }
    }

    private Basket() { }

    private Basket(Guid userId)
    {
        UserId = userId;
        _items = [];
    }

    [JsonConstructor]
    private Basket(
        Guid userId,
        List<BasketItem> items)
    {
        UserId = userId;
        _items = items?.ToList() ?? [];
    }

    public static Result<Basket> Create(Guid userId)
    {
        if (userId == Guid.Empty)
            return Result.Failure<Basket>(BasketErrors.InvalidUserId);

        return Result.Success(new Basket(userId));
    }

    public static Result<Basket> Create(
        Guid userId,
        IEnumerable<BasketItem> items)
    {
        if (userId == Guid.Empty)
            return Result.Failure<Basket>(BasketErrors.InvalidUserId);

        return Result.Success(new Basket(userId, items?.ToList() ?? []));
    }

    public Result<BasketItem> AddItem(
        ProductId productId,
        string productName,
        string pictureUrl,
        Money price,
        int quantity)
    {
        if (productId == default)
            return Result.Failure<BasketItem>(BasketErrors.InvalidProductId);

        if (price is null)
            return Result.Failure<BasketItem>(BasketErrors.PriceRequired);

        if (quantity < 1)
            return Result.Failure<BasketItem>(BasketErrors.InvalidQuantity);

        if (_items.Count > 0 && _items[0].Price.Currency != price.Currency)
            return Result.Failure<BasketItem>(BasketErrors.CurrencyMismatch);

        var existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is not null)
        {
            var priceResult = existing.UpdatePrice(price);
            if (priceResult.IsFailure)
                return Result.Failure<BasketItem>(priceResult.Error);

            var increaseResult = existing.IncreaseQuantity(quantity);
            if (increaseResult.IsFailure)
                return Result.Failure<BasketItem>(increaseResult.Error);

            return Result.Success(existing);
        }

        var itemResult = BasketItem.Create(
            productId,
            productName,
            pictureUrl,
            price,
            quantity);

        if (itemResult.IsFailure)
            return Result.Failure<BasketItem>(itemResult.Error);

        _items.Add(itemResult.Value);
        return Result.Success(itemResult.Value);
    }

    public Result RemoveItem(ProductId productId)
    {
        var existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is null)
            return Result.Failure(BasketErrors.ItemNotFound);

        _items.Remove(existing);
        return Result.Success();
    }

    public Result UpdateQuantity(ProductId productId, int quantity)
    {
        if (quantity < 1)
            return Result.Failure(BasketErrors.InvalidQuantity);

        var existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is null)
            return Result.Failure(BasketErrors.ItemNotFound);

        return existing.ChangeQuantity(quantity);
    }

    public Result MergeFrom(Basket other)
    {
        if (other is null)
            return Result.Failure(BasketErrors.MergeSourceRequired);

        if (other.UserId == UserId)
            return Result.Failure(BasketErrors.CannotMergeSelf);

        foreach (var item in other.Items)
        {
            var result = AddItem(
                item.ProductId,
                item.ProductName,
                item.PictureUrl,
                item.Price,
                item.Quantity);

            if (result.IsFailure)
                return Result.Failure(result.Error);
        }

        return Result.Success();
    }

    public void Clear()
    {
        _items.Clear();
    }
}
