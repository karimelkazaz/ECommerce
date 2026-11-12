using ECommerce.Domain.Entities.Orders;
using MediatR;

namespace ECommerce.Application.Orders;

/// <summary>
/// Follow-up message ordering the "order cancelled" email. Enqueued by
/// <see cref="EventHandlers.OrderCancelledDomainEventHandler"/> with the
/// recipient resolved up front so the sender consumer merely formats and
/// delivers. The reason is optional (e.g. an order auto-cancelled for
/// non-payment carries the expiry reason; it may be null when blanked).
/// </summary>
public sealed record OrderCancelledEmailNotification(
    OrderId OrderId,
    string CustomerEmail,
    string CustomerFirstName,
    decimal TotalAmount,
    string CurrencyCode,
    string? CancellationReason) : INotification;
