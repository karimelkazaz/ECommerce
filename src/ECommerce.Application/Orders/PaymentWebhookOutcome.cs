using ECommerce.Domain.Common;

namespace ECommerce.Application.Orders;

public sealed record PaymentIntentResult(
    string PaymentIntentId,
    string ClientSecret,
    long AmountMinorUnits,
    string CurrencyCode);

public enum PaymentOutcome
{
    Unhandled,
    Succeeded,
    Failed,
    Canceled,
    Refunded
}

public sealed record PaymentWebhookOutcome(
    PaymentOutcome Outcome,
    string? PaymentIntentId,
    string? FailureReason,
    string EventId,
    string EventType);

public sealed record WebhookOutcomeResponse(
    bool Processed,
    string Outcome,
    string? PaymentIntentId);

public static class PaymentErrors
{
    public static readonly Error InvalidSignature = Error.Validation(
        "Payment.InvalidSignature",
        "Stripe webhook signature is invalid or missing.");

    public static readonly Error UnhandledEvent = Error.Problem(
        "Payment.UnhandledEvent",
        "The webhook event is not handled by this API.");
}
