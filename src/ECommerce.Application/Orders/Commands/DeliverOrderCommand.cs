using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Orders;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.Orders.Commands;

public sealed record DeliverOrderCommand(Guid Id) : IRequest<Result>;

public sealed class DeliverOrderCommandHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork) : IRequestHandler<DeliverOrderCommand, Result>
{
    public async Task<Result> Handle(DeliverOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(new OrderId(request.Id), cancellationToken);
        if (order is null)
            return Result.Failure(OrderErrors.NotFound);

        var result = order.DeliverOrder();
        if (result.IsFailure)
            return Result.Failure(result.Error);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class DeliverOrderCommandValidator : AbstractValidator<DeliverOrderCommand>
{
    public DeliverOrderCommandValidator()
    {
        RuleFor(c => c.Id).NotEqual(Guid.Empty);
    }
}
