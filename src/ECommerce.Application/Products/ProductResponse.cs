using System.Linq.Expressions;
using ECommerce.Domain.Entities.Products;

namespace ECommerce.Application.Products;

public sealed record ProductResponse(
    Guid Id,
    string Name,
    string Description,
    string PictureUrl,
    decimal Price,
    string Currency,
    string BrandName,
    string TypeName)
{
    public static readonly Expression<Func<Product, ProductResponse>> Projection = p => new ProductResponse(
        p.Id.Value,
        p.Name,
        p.Description,
        p.PictureUrl,
        p.Price.Amount,
        p.Price.Currency.Code,
        p.ProductBrand.Name,
        p.ProductType.Name);
}
