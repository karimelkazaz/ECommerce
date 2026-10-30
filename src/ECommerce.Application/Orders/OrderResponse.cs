using ECommerce.Domain.Entities.Orders;

namespace ECommerce.Application.Orders;

public sealed record OrderItemResponse(
    Guid ProductId,
    string ProductName,
    string PictureUrl,
    decimal Price,
    string Currency,
    int Quantity,
    decimal LineTotal);

public sealed record OrderAddressResponse(
    string RecipientFirstName,
    string RecipientLastName,
    string PhoneNumber,
    string Country,
    string City,
    string Street,
    string PostalCode);

public sealed record OrderDeliveryMethodResponse(
    string Name,
    string DeliveryTime,
    decimal Price,
    string Currency);

public sealed record OrderResponse(
    Guid Id,
    Guid UserId,
    string Status,
    decimal SubTotal,
    decimal ShippingFee,
    decimal Total,
    string Currency,
    string? PaymentIntentId,
    DateTimeOffset? PaidAtUtc,
    DateTimeOffset? ShippedAtUtc,
    DateTimeOffset? DeliveredAtUtc,
    DateTimeOffset? CancelledAtUtc,
    string? CancellationReason,
    DateTimeOffset? RefundedAtUtc,
    DateTimeOffset CreatedOnUtc,
    OrderAddressResponse ShippingAddress,
    OrderDeliveryMethodResponse DeliveryMethod,
    IReadOnlyList<OrderItemResponse> Items)
{
    public static OrderResponse FromDomain(Order order) => new(
        order.Id.Value,
        order.UserId,
        order.Status.ToString(),
        order.SubTotal.Amount,
        order.ShippingFee.Amount,
        order.Total.Amount,
        order.Total.Currency.Code,
        order.PaymentIntentId,
        order.PaidAtUtc,
        order.ShippedAtUtc,
        order.DeliveredAtUtc,
        order.CancelledAtUtc,
        order.CancellationReason,
        order.RefundedAtUtc,
        order.CreatedOnUtc,
        new OrderAddressResponse(
            order.ShippingAddress.RecipientFirstName,
            order.ShippingAddress.RecipientLastName,
            order.ShippingAddress.PhoneNumber,
            order.ShippingAddress.Country,
            order.ShippingAddress.City,
            order.ShippingAddress.Street,
            order.ShippingAddress.PostalCode),
        new OrderDeliveryMethodResponse(
            order.DeliveryMethod.Name,
            order.DeliveryMethod.DeliveryTime,
            order.DeliveryMethod.Price.Amount,
            order.DeliveryMethod.Price.Currency.Code),
        order.OrderItems
            .OrderBy(i => i.Id.Value)
            .Select(i => new OrderItemResponse(
                i.ProductId.Value,
                i.ProductName,
                i.PictureUrl,
                i.Price.Amount,
                i.Price.Currency.Code,
                i.Quantity,
                i.Price.Amount * i.Quantity))
            .ToList());
}

public sealed record CheckoutResponse(
    Guid OrderId,
    string Status,
    decimal Total,
    string Currency,
    string? PaymentIntentId,
    string? ClientSecret);
