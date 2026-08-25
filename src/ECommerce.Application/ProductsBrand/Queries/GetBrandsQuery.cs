using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.ProductsBrand;
using ECommerce.Domain.Entities.ProductsBrand;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.ProductsBrand.Queries;

public sealed record GetBrandsQuery : IRequest<IReadOnlyList<BrandResponse>>;

public sealed class GetBrandsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetBrandsQuery, IReadOnlyList<BrandResponse>>
{
    public async Task<IReadOnlyList<BrandResponse>> Handle(GetBrandsQuery request, CancellationToken cancellationToken)
        => await db.Brands
            .OrderBy(b => b.Name)
            .Select(b => new BrandResponse(b.Id.Value, b.Name))
            .ToListAsync(cancellationToken);
}
