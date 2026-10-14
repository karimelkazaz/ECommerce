using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Baskets;
using ECommerce.Domain.Repositories;
using MediatR;

namespace ECommerce.Application.Baskets.Queries;

public sealed record GetBasketQuery(Guid? UserId = null) : IRequest<Result<BasketResponse>>;

public sealed class GetBasketQueryHandler(
    IBasketStore basketStore,
    ICurrentUserService currentUserService) : IRequestHandler<GetBasketQuery, Result<BasketResponse>>
{
    public async Task<Result<BasketResponse>> Handle(GetBasketQuery request, CancellationToken cancellationToken)
    {
        var userId = request.UserId ?? currentUserService.UserId;
        if (userId is null || userId == Guid.Empty)
            return Result.Failure<BasketResponse>(BasketErrors.InvalidUserId);

        var basket = await basketStore.GetOrCreateAsync(userId.Value, cancellationToken);
        return BasketResponse.FromDomain(basket);
    }
}

