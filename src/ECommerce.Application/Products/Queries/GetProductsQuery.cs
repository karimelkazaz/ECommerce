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
    int PageSize = 10,
    ProductSortField SortBy = ProductSortField.Name,
    bool SortDescending = false) : IRequest<PagedResponse<ProductResponse>>;

public enum ProductSortField
{
    Name,
    Price,
    Brand,
    Type
}

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

        if (request.BrandId.HasValue && request.BrandId != Guid.Empty)
            query = query.Where(p => p.ProductBrandId == new ProductBrandId(request.BrandId.Value));

        if (request.TypeId.HasValue && request.TypeId != Guid.Empty)
            query = query.Where(p => p.ProductTypeId == new ProductTypeId(request.TypeId.Value));

        query = ApplySort(query, request.SortBy, request.SortDescending);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ProductResponse.Projection)
            .ToListAsync(cancellationToken);

        return new PagedResponse<ProductResponse>(items, page, pageSize, totalCount);
    }

    private static IQueryable<Product> ApplySort(
        IQueryable<Product> query,
        ProductSortField sortBy,
        bool sortDescending) 
    {
        return sortBy switch
        {
            ProductSortField.Price => sortDescending
                ? query.OrderByDescending(x => x.Price.Amount).ThenBy(x => x.Name)
                : query.OrderBy(x => x.Price.Amount).ThenBy(x => x.Name),

            ProductSortField.Brand => sortDescending
                ? query.OrderByDescending(x => x.ProductBrand.Name).ThenBy(x => x.Name)
                : query.OrderBy(x => x.ProductBrand.Name).ThenBy(x => x.Name),

            ProductSortField.Type => sortDescending
                ? query.OrderByDescending(x => x.ProductType.Name).ThenBy(x => x.Name)
                : query.OrderBy(x => x.ProductType.Name).ThenBy(x => x.Name),

            _ => sortDescending
                ? query.OrderByDescending(x => x.Name)
                : query.OrderBy(x => x.Name)
        };
    }
}
