using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Orders;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Orders.Commands;

public sealed record HandlePaymentFailedCommand(string PaymentIntentId, string? Reason = null) : IRequest<Result>;

public sealed class HandlePaymentFailedCommandHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    ILogger<HandlePaymentFailedCommandHandler> logger) : IRequestHandler<HandlePaymentFailedCommand, Result>
{
    public async Task<Result> Handle(HandlePaymentFailedCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PaymentIntentId))
            return Result.Failure(OrderErrors.InvalidPaymentIntent);

        var order = await orders.GetByPaymentIntentIdAsync(request.PaymentIntentId, cancellationToken);
        if (order is null)
        {
            logger.LogWarning(
                "Payment {Outcome} for unknown order (PaymentIntentId {PaymentIntentId}).",
                request.Reason ?? "failed",
                request.PaymentIntentId);

            return Result.Failure(OrderErrors.NotFound);
        }

        var result = order.MarkAsPaymentFailed();
        if (result.IsFailure)
        {
            // e.g. the payment actually succeeded and the order advanced — idempotent no-op.
            logger.LogInformation(
                "Order {OrderId} ignored payment failure (PaymentIntentId {PaymentIntentId}): {ErrorCode}.",
                order.Id.Value,
                request.PaymentIntentId,
                result.Error.Code);

            return Result.Success();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
