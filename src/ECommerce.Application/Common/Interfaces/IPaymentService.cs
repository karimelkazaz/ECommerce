using ECommerce.Application.Orders;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Orders;

namespace ECommerce.Application.Common.Interfaces;

public interface IPaymentService
{
    Task<Result<PaymentIntentResult>> CreatePaymentIntentAsync(Order order, CancellationToken ct = default);

    /// <summary>Best effort: cancels the intent so a stale client secret can no longer complete payment.</summary>
    Task CancelPaymentIntentAsync(string paymentIntentId, CancellationToken ct = default);

    /// <summary>Verifies the webhook signature and normalizes the event into a payment outcome.</summary>
    Task<Result<PaymentWebhookOutcome>> HandleWebhookAsync(string json, string signatureHeader, CancellationToken ct = default);
}
