using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Baskets;
using ECommerce.Domain.Repositories;
using MediatR;

namespace ECommerce.Application.Baskets.Commands;

public sealed record ClearBasketCommand(Guid? UserId = null) : IRequest<Result>;

public sealed class ClearBasketCommandHandler(
    IBasketStore basketStore,
    ICurrentUserService currentUserService) : IRequestHandler<ClearBasketCommand, Result>
{
    public async Task<Result> Handle(ClearBasketCommand request, CancellationToken cancellationToken)
    {
        var userId = request.UserId ?? currentUserService.UserId;
        if (userId is null || userId == Guid.Empty)
            return Result.Failure(BasketErrors.InvalidUserId);

        await basketStore.DeleteAsync(userId.Value, cancellationToken);
        return Result.Success();
    }
}

