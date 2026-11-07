using ECommerce.Application.Common.EventHandlers;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Orders.Emails;
using ECommerce.Domain.Common;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Orders.EventHandlers;

public sealed class OrderPaymentFailedEmailRequestHandler(
    IInbox inbox,
    IUnitOfWork unitOfWork,
    IOutboxMessageContext messageContext,
    IEmailSender emailSender,
    ILogger<OrderPaymentFailedEmailRequestHandler> logger)
    : IdempotentNotificationHandler<OrderPaymentFailedEmailNotification>(inbox, unitOfWork, messageContext, logger)
{
    protected override string ConsumerId => "orders.order-payment-failed-email";

    protected override async Task HandleCore(
        OrderPaymentFailedEmailNotification notification,
        CancellationToken cancellationToken)
    {
        var (subject, htmlBody) = OrderConfirmationEmailBuilder.BuildPaymentFailed(notification);

        await emailSender.SendAsync(notification.CustomerEmail, subject, htmlBody, cancellationToken);

        logger.LogInformation(
            "Payment failed email sent for order {OrderId} to {Email}.",
            notification.OrderId.Value,
            notification.CustomerEmail);
    }
}
