using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Orders;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Orders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;

namespace ECommerce.Infrastructure.Payment;

public sealed class StripePaymentService(
    IOptions<StripeOptions> options,
    ILogger<StripePaymentService> logger) : IPaymentService
{
    private readonly StripeOptions _options = options.Value;

    public async Task<Result<PaymentIntentResult>> CreatePaymentIntentAsync(Order order, CancellationToken ct = default)
    {
        StripeConfiguration.ApiKey = _options.SecretKey;

        var amountMinorUnits = ToMinorUnits(order.Total.Amount);
        if (amountMinorUnits < 1)
            return Result.Failure<PaymentIntentResult>(OrderErrors.InvalidTotal);

        var intentOptions = new PaymentIntentCreateOptions
        {
            Amount = amountMinorUnits,
            Currency = order.Total.Currency.Code.ToLowerInvariant(),
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
            },
            Metadata = new Dictionary<string, string>
            {
                ["order_id"] = order.Id.Value.ToString(),
                ["user_id"] = order.UserId.ToString(),
            },
        };

        // Retries (e.g. timeouts) reuse the same key instead of creating a second intent.
        var requestOptions = new RequestOptions { IdempotencyKey = $"order:{order.Id.Value}" };

        try
        {
            var intent = await new PaymentIntentService().CreateAsync(intentOptions, requestOptions, ct);

            return Result.Success(new PaymentIntentResult(
                intent.Id,
                intent.ClientSecret,
                amountMinorUnits,
                intent.Currency));
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Failed to create payment intent for order {OrderId}.", order.Id.Value);

            return Result.Failure<PaymentIntentResult>(Error.Problem(
                "Payment.StripeCreateFailed",
                ex.StripeError?.Message ?? ex.Message));
        }
    }

    public async Task CancelPaymentIntentAsync(string paymentIntentId, CancellationToken ct = default)
    {
        StripeConfiguration.ApiKey = _options.SecretKey;

        try
        {
            await new PaymentIntentService().CancelAsync(
                paymentIntentId,
                new PaymentIntentCancelOptions(),
                new RequestOptions { IdempotencyKey = $"cancel:{paymentIntentId}" },
                ct);
        }
        catch (StripeException ex)
        {
            // Best effort — a paid or already-cancelled intent must not fail the checkout flow.
            logger.LogWarning(
                ex,
                "Could not cancel payment intent {PaymentIntentId}; the associated order ignores late payment events.",
                paymentIntentId);
        }
    }

    public async Task<Result<PaymentWebhookOutcome>> HandleWebhookAsync(string json, string signatureHeader, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader))
            return Result.Failure<PaymentWebhookOutcome>(PaymentErrors.InvalidSignature);

        StripeConfiguration.ApiKey = _options.SecretKey;

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, _options.WebhookSecret);
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Stripe webhook signature verification failed.");

            return Result.Failure<PaymentWebhookOutcome>(Error.Problem(
                "Payment.InvalidSignature",
                ex.StripeError?.Message ?? ex.Message));
        }

        var paymentIntent = stripeEvent.Data?.Object as PaymentIntent;

        var outcome = stripeEvent.Type switch
        {
            "payment_intent.succeeded" => PaymentOutcome.Succeeded,
            "payment_intent.payment_failed" => PaymentOutcome.Failed,
            "payment_intent.canceled" => PaymentOutcome.Canceled,
            "payment_intent.refunded" => PaymentOutcome.Refunded,
            _ => PaymentOutcome.Unhandled,
        };

        await Task.CompletedTask;

        return Result.Success(new PaymentWebhookOutcome(
            outcome,
            paymentIntent?.Id,
            paymentIntent?.LastPaymentError?.Message,
            stripeEvent.Id,
            stripeEvent.Type));
    }

    // All currently supported currencies (EGP/USD/EUR) use two decimal places.
    private static long ToMinorUnits(decimal amount)
        => (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero);
}
