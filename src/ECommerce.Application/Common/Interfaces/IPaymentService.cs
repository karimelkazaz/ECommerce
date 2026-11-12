using ECommerce.Application.Orders;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Orders;

namespace ECommerce.Application.Common.Interfaces;

public interface IPaymentService
{
    Task<Result<PaymentIntentResult>> CreatePaymentIntentAsync(Order order, CancellationToken ct = default);

    Task CancelPaymentIntentAsync(string paymentIntentId, CancellationToken ct = default);

    Task<Result> CreateRefundAsync(string paymentIntentId, CancellationToken ct = default);

    Task<Result<PaymentWebhookOutcome>> HandleWebhookAsync(string json, string signatureHeader, CancellationToken ct = default);
}
