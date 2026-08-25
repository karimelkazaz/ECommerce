using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.ProductsType;
using ECommerce.Domain.Entities.ProductsType;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.ProductsType.Queries;

public sealed record GetTypesQuery : IRequest<IReadOnlyList<TypeResponse>>;

public sealed class GetTypesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetTypesQuery, IReadOnlyList<TypeResponse>>
{
    public async Task<IReadOnlyList<TypeResponse>> Handle(GetTypesQuery request, CancellationToken cancellationToken)
        => await db.Types
            .OrderBy(t => t.Name)
            .Select(t => new TypeResponse(t.Id.Value, t.Name))
            .ToListAsync(cancellationToken);
}
