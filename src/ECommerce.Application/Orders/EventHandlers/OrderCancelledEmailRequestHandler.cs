using ECommerce.Application.Common.EventHandlers;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Orders.Emails;
using ECommerce.Domain.Common;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Orders.EventHandlers;

public sealed class OrderCancelledEmailRequestHandler(
    IInbox inbox,
    IUnitOfWork unitOfWork,
    IOutboxMessageContext messageContext,
    IEmailSender emailSender,
    ILogger<OrderCancelledEmailRequestHandler> logger)
    : IdempotentNotificationHandler<OrderCancelledEmailNotification>(inbox, unitOfWork, messageContext, logger)
{
    protected override string ConsumerId => "orders.order-cancelled-email";

    protected override async Task HandleCore(
        OrderCancelledEmailNotification notification,
        CancellationToken cancellationToken)
    {
        var (subject, htmlBody) = OrderConfirmationEmailBuilder.BuildCancelled(notification);

        await emailSender.SendAsync(notification.CustomerEmail, subject, htmlBody, cancellationToken);

        logger.LogInformation(
            "Cancellation email sent for order {OrderId} to {Email}.",
            notification.OrderId.Value,
            notification.CustomerEmail);
    }
}
