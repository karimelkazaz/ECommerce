using ECommerce.Application.Common.EventHandlers;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Orders;
using ECommerce.Domain.Entities.Orders.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Orders.EventHandlers;

public sealed class OrderPaidDomainEventHandler(
    IInbox inbox,
    IUnitOfWork unitOfWork,
    IOutboxMessageContext messageContext,
    IApplicationDbContext db,
    ISalesSummaryRepository sales,
    ILogger<OrderPaidDomainEventHandler> logger)
    : IdempotentNotificationHandler<OrderPaidDomainEvent>(inbox, unitOfWork, messageContext, logger)
{
    protected override string ConsumerId => "orders.order-paid";

    protected override async Task HandleCore(
        OrderPaidDomainEvent notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Order {OrderId} was paid.", notification.OrderId.Value);

        // Read-side accrual: revenue/order-count per UTC day per currency. The
        // claim dominates exactly-once semantics; nothing is enqueued here.
        var order = await db.Orders
            .FirstOrDefaultAsync(o => o.Id == notification.OrderId, cancellationToken);

        if (order?.PaidAtUtc is null)
        {
            logger.LogWarning(
                "Paid order {OrderId} not found or missing PaidAtUtc; sales summary skipped.",
                notification.OrderId.Value);
            return;
        }

        var day = DateOnly.FromDateTime(order.PaidAtUtc.Value.UtcDateTime);
        var currencyCode = order.Total.Currency.Code;

        var existing = await sales.GetByDayAsync(day, currencyCode, cancellationToken);
        if (existing is null)
        {
            var startResult = SalesSummary.Start(day, order.Total);
            if (startResult.IsFailure)
            {
                logger.LogWarning(
                    "Could not start a sales summary for {Day} ({Currency}): {Error}",
                    day, currencyCode, startResult.Error.Code);
                return;
            }

            sales.Add(startResult.Value);
        }
        else
        {
            existing.ApplySale(order.Total);
        }

        logger.LogInformation(
            "Sales summary {Day} ({Currency}) updated from order {OrderId}.",
            day, currencyCode, notification.OrderId.Value);
    }
}
