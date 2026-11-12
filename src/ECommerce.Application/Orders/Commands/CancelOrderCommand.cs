using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Orders;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.Orders.Commands;

public sealed record CancelOrderCommand(Guid Id, string? Reason = null) : IRequest<Result>;

public sealed class CancelOrderCommandHandler(
    IOrderRepository orders,
    IPaymentService paymentService,
    IUnitOfWork unitOfWork) : IRequestHandler<CancelOrderCommand, Result>
{
    public async Task<Result> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(new OrderId(request.Id), cancellationToken);
        if (order is null)
            return Result.Failure(OrderErrors.NotFound);

        // A paid (Processing) order must get its money back: initiate the
        // refund first and gate the cancellation on Stripe accepting it, so a
        // failed refund never strands a cancelled order with captured funds.
        if (order.Status == OrderStatus.Processing
            && !string.IsNullOrWhiteSpace(order.PaymentIntentId))
        {
            var refundResult = await paymentService.CreateRefundAsync(order.PaymentIntentId, cancellationToken);
            if (refundResult.IsFailure)
                return Result.Failure(refundResult.Error);
        }

        var result = order.CancelOrder(request.Reason ?? "Cancelled by admin");
        if (result.IsFailure)
            return Result.Failure(result.Error);

        if (order.PaidAtUtc is not null)
        {
            _ = order.Refund();
        }

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
