using ECommerce.Application.Common.EventHandlers;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Orders.Emails;
using ECommerce.Domain.Common;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Orders.EventHandlers;

public sealed class OrderConfirmationEmailRequestHandler(
    IInbox inbox,
    IUnitOfWork unitOfWork,
    IOutboxMessageContext messageContext,
    IEmailSender emailSender,
    ILogger<OrderConfirmationEmailRequestHandler> logger)
    : IdempotentNotificationHandler<OrderConfirmationEmailNotification>(inbox, unitOfWork, messageContext, logger)
{
    protected override string ConsumerId => "orders.order-confirmation-email";

    protected override async Task HandleCore(
        OrderConfirmationEmailNotification notification,
        CancellationToken cancellationToken)
    {
        var (subject, htmlBody) = OrderConfirmationEmailBuilder.BuildOrderConfirmation(notification);

        await emailSender.SendAsync(notification.CustomerEmail, subject, htmlBody, cancellationToken);

        logger.LogInformation(
            "Order confirmation email sent for order {OrderId} to {Email}.",
            notification.OrderId.Value,
            notification.CustomerEmail);
    }
}
