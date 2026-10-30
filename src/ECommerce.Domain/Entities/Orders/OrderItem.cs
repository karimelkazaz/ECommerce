using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities.Orders;

public class OrderItem : Entity<OrderItemId>
{
    public ProductId ProductId { get; private set; }
    public string ProductName { get; private set; } = null!;
    public string PictureUrl { get; private set; } = null!;
    public Money Price { get; private set; } = null!;
    public int Quantity { get; private set; }

    private OrderItem() { }

    internal OrderItem(
        OrderItemId id,
        ProductId productId,
        string productName,
        string pictureUrl,
        Money price,
        int quantity) : base(id)
    {
        ProductId = productId;
        ProductName = productName;
        PictureUrl = pictureUrl;
        Price = price;
        Quantity = quantity;
    }

    internal static Result<OrderItem> Create(
        OrderItemId id,
        ProductId productId,
        string productName,
        string pictureUrl,
        Money price,
        int quantity)
    {
        if (id == default)
            return Result.Failure<OrderItem>(OrderErrors.InvalidItemId);

        if (productId == default)
            return Result.Failure<OrderItem>(OrderErrors.InvalidProductId);

        if (quantity < 1)
            return Result.Failure<OrderItem>(OrderErrors.InvalidQuantity);

        if (price is null)
            return Result.Failure<OrderItem>(OrderErrors.InvalidItemPrice);

        return Result.Success(new OrderItem(
            id,
            productId,
            productName,
            pictureUrl,
            price,
            quantity));
    }
}
