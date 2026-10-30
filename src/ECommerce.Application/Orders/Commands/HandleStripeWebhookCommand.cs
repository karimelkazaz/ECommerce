using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Orders;
using ECommerce.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Orders.Commands;

public sealed record HandleStripeWebhookCommand(string Json, string SignatureHeader) : IRequest<Result<WebhookOutcomeResponse>>;

public sealed class HandleStripeWebhookCommandHandler(
    IPaymentService paymentService,
    ISender sender,
    ILogger<HandleStripeWebhookCommandHandler> logger) : IRequestHandler<HandleStripeWebhookCommand, Result<WebhookOutcomeResponse>>
{
    public async Task<Result<WebhookOutcomeResponse>> Handle(HandleStripeWebhookCommand request, CancellationToken cancellationToken)
    {
        var outcomeResult = await paymentService.HandleWebhookAsync(request.Json, request.SignatureHeader, cancellationToken);
        if (outcomeResult.IsFailure)
            return Result.Failure<WebhookOutcomeResponse>(outcomeResult.Error);

        var outcome = outcomeResult.Value;

        if (outcome.Outcome == PaymentOutcome.Unhandled || string.IsNullOrWhiteSpace(outcome.PaymentIntentId))
        {
            logger.LogInformation(
                "Stripe webhook {EventId} ({EventType}) has no matching handling; no changes applied.",
                outcome.EventId,
                outcome.EventType);

            return Result.Success(new WebhookOutcomeResponse(false, outcome.Outcome.ToString(), outcome.PaymentIntentId));
        }

        Result dispatchResult = outcome.Outcome switch
        {
            PaymentOutcome.Succeeded => await sender.Send(new HandlePaymentSucceededCommand(outcome.PaymentIntentId), cancellationToken),
            PaymentOutcome.Failed or PaymentOutcome.Canceled => await sender.Send(new HandlePaymentFailedCommand(outcome.PaymentIntentId, outcome.FailureReason), cancellationToken),
            PaymentOutcome.Refunded => await sender.Send(new HandleRefundedCommand(outcome.PaymentIntentId), cancellationToken),
            _ => Result.Success(),
        };

        if (dispatchResult.IsFailure)
        {
            // The signature was valid, so never return a failure to Stripe — it would keep retrying.
            logger.LogError(
                "Stripe webhook {EventId} ({EventType}) could not be processed: {ErrorCode} {ErrorDescription}.",
                outcome.EventId,
                outcome.EventType,
                dispatchResult.Error.Code,
                dispatchResult.Error.Description);

            return Result.Success(new WebhookOutcomeResponse(false, outcome.Outcome.ToString(), outcome.PaymentIntentId));
        }

        logger.LogInformation(
            "Stripe webhook {EventId} ({EventType}) processed: {PaymentIntentId} -> {Outcome}.",
            outcome.EventId,
            outcome.EventType,
            outcome.PaymentIntentId,
            outcome.Outcome);

        return Result.Success(new WebhookOutcomeResponse(true, outcome.Outcome.ToString(), outcome.PaymentIntentId));
    }
}
