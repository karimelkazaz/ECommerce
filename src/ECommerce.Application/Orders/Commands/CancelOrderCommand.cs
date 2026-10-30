using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Orders;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.Orders.Commands;

public sealed record CancelOrderCommand(Guid Id, string? Reason = null) : IRequest<Result>;

public sealed class CancelOrderCommandHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork) : IRequestHandler<CancelOrderCommand, Result>
{
    public async Task<Result> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(new OrderId(request.Id), cancellationToken);
        if (order is null)
            return Result.Failure(OrderErrors.NotFound);

        var result = order.CancelOrder(request.Reason ?? "Cancelled by admin");
        if (result.IsFailure)
            return Result.Failure(result.Error);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(c => c.Id).NotEqual(Guid.Empty);
    }
}
