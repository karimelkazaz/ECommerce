using ECommerce.Application.Common;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.Entities.ProductsBrand;
using ECommerce.Domain.Entities.ProductsType;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Products.Queries;

public sealed record GetProductsQuery(
    string? Search = null,
    Guid? BrandId = null,
    Guid? TypeId = null,
    int Page = 1,
    int PageSize = 10) : IRequest<PagedResponse<ProductResponse>>;

public sealed class GetProductsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetProductsQuery, PagedResponse<ProductResponse>>
{
    public async Task<PagedResponse<ProductResponse>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        IQueryable<Product> query = db.Products;

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(p => p.Name.Contains(request.Search));

        if (request.BrandId is { } brand && brand != Guid.Empty)
            query = query.Where(p => p.ProductBrandId == new ProductBrandId(brand));

        if (request.TypeId is { } type && type != Guid.Empty)
            query = query.Where(p => p.ProductTypeId == new ProductTypeId(type));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductResponse(
                p.Id.Value,
                p.Name,
                p.Description,
                p.PictureUrl,
                p.Price.Amount,
                p.Price.Currency.Code,
                p.ProductBrand.Name,
                p.ProductType.Name))
            .ToListAsync(cancellationToken);

        return new PagedResponse<ProductResponse>(items, page, pageSize, totalCount);
    }
}
