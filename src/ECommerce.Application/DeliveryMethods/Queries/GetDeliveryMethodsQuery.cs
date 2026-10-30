using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.DeliveryMethods;
using ECommerce.Domain.Entities.DeliveryMethods;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.DeliveryMethods.Queries;

public sealed record GetDeliveryMethodsQuery : IRequest<IReadOnlyList<DeliveryMethodResponse>>;

public sealed class GetDeliveryMethodsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetDeliveryMethodsQuery, IReadOnlyList<DeliveryMethodResponse>>
{
    public async Task<IReadOnlyList<DeliveryMethodResponse>> Handle(GetDeliveryMethodsQuery request, CancellationToken cancellationToken)
        => await db.DeliveryMethods
            .OrderBy(d => d.Name)
            .Select(d => new DeliveryMethodResponse(
                d.Id.Value,
                d.Name,
                d.DeliveryTime,
                d.Price.Amount,
                d.Price.Currency.Code,
                d.Description,
                d.IsAvailable))
            .ToListAsync(cancellationToken);
}
