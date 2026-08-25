using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.ProductsType;
using ECommerce.Domain.Entities.ProductsType;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.ProductsType.Queries;

public sealed record GetTypeByIdQuery(Guid Id) : IRequest<TypeResponse?>;

public sealed class GetTypeByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetTypeByIdQuery, TypeResponse?>
{
    public async Task<TypeResponse?> Handle(GetTypeByIdQuery request, CancellationToken cancellationToken)
    {
        var typeId = new ProductTypeId(request.Id);

        return await db.Types
            .Where(t => t.Id == typeId)
            .Select(t => new TypeResponse(t.Id.Value, t.Name))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
