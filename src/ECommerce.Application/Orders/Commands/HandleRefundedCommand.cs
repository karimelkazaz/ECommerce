using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Orders;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Orders.Commands;

public sealed record HandleRefundedCommand(string PaymentIntentId) : IRequest<Result>;

public sealed class HandleRefundedCommandHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    ILogger<HandleRefundedCommandHandler> logger) : IRequestHandler<HandleRefundedCommand, Result>
{
    public async Task<Result> Handle(HandleRefundedCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PaymentIntentId))
            return Result.Failure(OrderErrors.InvalidPaymentIntent);

        var order = await orders.GetByPaymentIntentIdAsync(request.PaymentIntentId, cancellationToken);
        if (order is null)
        {
            logger.LogWarning(
                "Payment refunded for unknown order (PaymentIntentId {PaymentIntentId}).",
                request.PaymentIntentId);

            return Result.Failure(OrderErrors.NotFound);
        }

        var result = order.Refund();
        if (result.IsFailure)
        {
            logger.LogInformation(
                "Order {OrderId} ignored refund (PaymentIntentId {PaymentIntentId}): {ErrorCode}.",
                order.Id.Value,
                request.PaymentIntentId,
                result.Error.Code);

            return Result.Success();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
