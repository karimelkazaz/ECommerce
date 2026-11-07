using ECommerce.Domain.Entities.Orders;
using MediatR;

namespace ECommerce.Application.Orders;

/// <summary>
/// Follow-up message ordering the "payment did not go through" email.
/// Enqueued by <see cref="EventHandlers.OrderPaymentFailedDomainEventHandler"/>
/// with the recipient resolved up front so the sender consumer merely formats
/// and delivers.
/// </summary>
public sealed record OrderPaymentFailedEmailNotification(
    OrderId OrderId,
    string CustomerEmail,
    string CustomerFirstName,
    decimal TotalAmount,
    string CurrencyCode) : INotification;
