using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Orders;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.Orders.Commands;

public sealed record ShipOrderCommand(Guid Id) : IRequest<Result>;

public sealed class ShipOrderCommandHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork) : IRequestHandler<ShipOrderCommand, Result>
{
    public async Task<Result> Handle(ShipOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(new OrderId(request.Id), cancellationToken);
        if (order is null)
            return Result.Failure(OrderErrors.NotFound);

        var result = order.ShipOrder();
        if (result.IsFailure)
            return Result.Failure(result.Error);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class ShipOrderCommandValidator : AbstractValidator<ShipOrderCommand>
{
    public ShipOrderCommandValidator()
    {
        RuleFor(c => c.Id).NotEqual(Guid.Empty);
    }
}
