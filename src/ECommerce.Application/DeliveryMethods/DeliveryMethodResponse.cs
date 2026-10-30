namespace ECommerce.Application.DeliveryMethods;

public sealed record DeliveryMethodResponse(
    Guid Id,
    string Name,
    string DeliveryTime,
    decimal Price,
    string Currency,
    string? Description,
    bool IsAvailable);
