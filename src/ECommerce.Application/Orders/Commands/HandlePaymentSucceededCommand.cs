using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Orders;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Orders.Commands;

public sealed record HandlePaymentSucceededCommand(string PaymentIntentId) : IRequest<Result>;

public sealed class HandlePaymentSucceededCommandHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    ILogger<HandlePaymentSucceededCommandHandler> logger) : IRequestHandler<HandlePaymentSucceededCommand, Result>
{
    public async Task<Result> Handle(HandlePaymentSucceededCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PaymentIntentId))
            return Result.Failure(OrderErrors.InvalidPaymentIntent);

        var order = await orders.GetByPaymentIntentIdAsync(request.PaymentIntentId, cancellationToken);
        if (order is null)
        {
            logger.LogWarning(
                "Payment succeeded for unknown order (PaymentIntentId {PaymentIntentId}).",
                request.PaymentIntentId);

            return Result.Failure(OrderErrors.NotFound);
        }

        var result = order.MarkAsPaid(request.PaymentIntentId);
        if (result.IsFailure)
        {
            // e.g. webhook replay or a cancel/failed event raced ahead — treat as an idempotent no-op.
            logger.LogInformation(
                "Order {OrderId} ignored payment success (PaymentIntentId {PaymentIntentId}): {ErrorCode}.",
                order.Id.Value,
                request.PaymentIntentId,
                result.Error.Code);

            return Result.Success();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
