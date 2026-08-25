using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products;
using ECommerce.Domain.Entities.Products;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Products.Queries;

public sealed record GetProductByIdQuery(Guid Id) : IRequest<ProductResponse?>;

public sealed class GetProductByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetProductByIdQuery, ProductResponse?>
{
    public async Task<ProductResponse?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var productId = new ProductId(request.Id);

        return await db.Products
            .Where(p => p.Id == productId)
            .Select(p => new ProductResponse(
                p.Id.Value,
                p.Name,
                p.Description,
                p.PictureUrl,
                p.Price.Amount,
                p.Price.Currency.Code,
                p.ProductBrand.Name,
                p.ProductType.Name))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
