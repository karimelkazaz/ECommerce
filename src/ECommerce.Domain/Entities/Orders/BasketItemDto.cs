using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities.Orders;

public record BasketItemDto(
    ProductId ProductId,
    string ProductName,
    string PictureUrl,
    Money Price,
    int Quantity
);

