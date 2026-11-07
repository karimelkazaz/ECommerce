using ECommerce.Domain.Entities.Orders;
using MediatR;

namespace ECommerce.Application.Orders;

/// <summary>
/// Follow-up message ordering the order-confirmation email. Enqueued by
/// <see cref="EventHandlers.OrderCreatedDomainEventHandler"/> with everything
/// the sender needs resolved up front (recipient, item lines, totals), so the
/// sender consumer never re-reads aggregates. Prices carry the order's single
/// currency (the order aggregate enforces one currency per order).
/// </summary>
public sealed record OrderConfirmationEmailNotification(
    OrderId OrderId,
    string CustomerEmail,
    string CustomerFirstName,
    IReadOnlyList<EmailLine> Lines,
    decimal SubTotalAmount,
    decimal ShippingFeeAmount,
    decimal TotalAmount,
    string CurrencyCode) : INotification;

public sealed record EmailLine(string ProductName, int Quantity, decimal PriceAmount);
