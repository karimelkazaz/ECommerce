using ECommerce.Domain.Entities.Baskets;

namespace ECommerce.Application.Baskets;

public sealed record BasketResponse(
    Guid UserId,
    IReadOnlyList<BasketItemResponse> Items,
    int TotalQuantity,
    decimal? SubTotalAmount,
    string? Currency)
{
    public static BasketResponse FromDomain(Basket basket) => new(
        basket.UserId,
        basket.Items.Select(item => new BasketItemResponse(
            item.ProductId.Value,
            item.ProductName,
            item.PictureUrl,
            item.Price.Amount,
            item.Price.Currency.Code,
            item.Quantity,
            item.LineTotal)).ToList(),
        basket.TotalQuantity,
        basket.SubTotal?.Amount,
        basket.SubTotal?.Currency.Code);
}

public sealed record BasketItemResponse(
    Guid ProductId,
    string ProductName,
    string PictureUrl,
    decimal Price,
    string Currency,
    int Quantity,
    decimal LineTotal);

