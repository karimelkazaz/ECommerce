using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.DeliveryMethods;
using ECommerce.Domain.Entities.DeliveryMethods;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.DeliveryMethods.Queries;

public sealed record GetDeliveryMethodByIdQuery(Guid Id) : IRequest<DeliveryMethodResponse?>;

public sealed class GetDeliveryMethodByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetDeliveryMethodByIdQuery, DeliveryMethodResponse?>
{
    public async Task<DeliveryMethodResponse?> Handle(GetDeliveryMethodByIdQuery request, CancellationToken cancellationToken)
    {
        var deliveryMethodId = new DeliveryMethodId(request.Id);

        return await db.DeliveryMethods
            .Where(d => d.Id == deliveryMethodId)
            .Select(d => new DeliveryMethodResponse(
                d.Id.Value,
                d.Name,
                d.DeliveryTime,
                d.Price.Amount,
                d.Price.Currency.Code))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
