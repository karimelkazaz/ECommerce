namespace ECommerce.Application.Products;

public sealed record ProductResponse(
    Guid Id,
    string Name,
    string Description,
    string PictureUrl,
    decimal Price,
    string Currency,
    string BrandName,
    string TypeName);
