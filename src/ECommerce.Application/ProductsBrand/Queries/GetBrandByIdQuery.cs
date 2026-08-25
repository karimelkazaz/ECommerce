using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.ProductsBrand;
using ECommerce.Domain.Entities.ProductsBrand;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.ProductsBrand.Queries;

public sealed record GetBrandByIdQuery(Guid Id) : IRequest<BrandResponse?>;

public sealed class GetBrandByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetBrandByIdQuery, BrandResponse?>
{
    public async Task<BrandResponse?> Handle(GetBrandByIdQuery request, CancellationToken cancellationToken)
    {
        var brandId = new ProductBrandId(request.Id);

        return await db.Brands
            .Where(b => b.Id == brandId)
            .Select(b => new BrandResponse(b.Id.Value, b.Name))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
