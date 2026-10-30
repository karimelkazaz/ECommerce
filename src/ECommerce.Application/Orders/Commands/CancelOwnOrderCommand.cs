using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Orders;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.Orders.Commands;

/// <summary>
/// Cancels the current user's own order. Customers may only cancel unpaid,
/// not-yet-confirmed orders; cancelling a paid order goes through a refund instead.
/// </summary>
public sealed record CancelOwnOrderCommand(Guid Id) : IRequest<Result>;

public sealed class CancelOwnOrderCommandHandler(
    IOrderRepository orders,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<CancelOwnOrderCommand, Result>
{
    public async Task<Result> Handle(CancelOwnOrderCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result.Failure(OrderErrors.InvalidUserId);

        var order = await orders.GetByIdAsync(new OrderId(request.Id), cancellationToken);
        if (order is null || order.UserId != userId.Value)
            return Result.Failure(OrderErrors.NotFound);

        if (order.Status != OrderStatus.Pending)
            return Result.Failure(OrderErrors.InvalidStateTransition);

        var result = order.CancelOrder("Cancelled by customer");
        if (result.IsFailure)
            return Result.Failure(result.Error);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class CancelOwnOrderCommandValidator : AbstractValidator<CancelOwnOrderCommand>
{
    public CancelOwnOrderCommandValidator()
    {
        RuleFor(c => c.Id).NotEqual(Guid.Empty);
    }
}
