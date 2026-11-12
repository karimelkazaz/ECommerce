using System.Net;
using System.Text;
using ECommerce.Domain.Entities.Orders;

namespace ECommerce.Application.Orders.Emails;

internal static class OrderConfirmationEmailBuilder
{
    public static (string Subject, string HtmlBody) BuildOrderConfirmation(
        OrderConfirmationEmailNotification notification)
    {
        var subject = $"Order confirmation — {notification.OrderId.Value:N}";

        var body = new StringBuilder();
        body.Append("<p>Hi ").Append(Escape(notification.CustomerFirstName))
            .Append(", thanks for your order!</p>");

        body.Append("<table><tr><th>Item</th><th>Qty</th><th>Price</th></tr>");
        foreach (var line in notification.Lines)
        {
            body.Append("<tr><td>").Append(Escape(line.ProductName))
                .Append("</td><td>").Append(line.Quantity)
                .Append("</td><td>").Append(FormatAmount(line.PriceAmount, notification.CurrencyCode))
                .Append("</td></tr>");
        }

        body.Append("</table>");

        AppendTotal(body, notification.CurrencyCode, "Subtotal", notification.SubTotalAmount);
        AppendTotal(body, notification.CurrencyCode, "Shipping", notification.ShippingFeeAmount);
        AppendTotal(body, notification.CurrencyCode, "Total", notification.TotalAmount);

        return (subject, body.ToString());
    }

    public static (string Subject, string HtmlBody) BuildPaymentFailed(
        OrderPaymentFailedEmailNotification notification)
    {
        var subject = $"Payment failed — order {notification.OrderId.Value:N}";

        var body = new StringBuilder();
        body.Append("<p>Hi ").Append(Escape(notification.CustomerFirstName))
            .Append(", your payment for order <strong>")
            .Append(notification.OrderId.Value.ToString("N"))
            .Append("</strong> did not go through. Please retry checkout to keep the order.</p>");

        AppendTotal(body, notification.CurrencyCode, "Order total", notification.TotalAmount);

        return (subject, body.ToString());
    }

    public static (string Subject, string HtmlBody) BuildCancelled(
        OrderCancelledEmailNotification notification)
    {
        var subject = $"Order cancelled — {notification.OrderId.Value:N}";

        var body = new StringBuilder();
        body.Append("<p>Hi ").Append(Escape(notification.CustomerFirstName))
            .Append(", your order <strong>")
            .Append(notification.OrderId.Value.ToString("N"))
            .Append("</strong> has been cancelled.");

        if (!string.IsNullOrWhiteSpace(notification.CancellationReason))
        {
            body.Append(" Reason: ").Append(Escape(notification.CancellationReason)).Append('.');
        }

        body.Append("</p>");

        AppendTotal(body, notification.CurrencyCode, "Order total", notification.TotalAmount);

        return (subject, body.ToString());
    }

    private static void AppendTotal(StringBuilder body, string currencyCode, string label, decimal amount)
        => body.Append("<p><strong>").Append(Escape(label))
            .Append(":</strong> ").Append(FormatAmount(amount, currencyCode))
            .Append("</p>");

    private static string FormatAmount(decimal amount, string currencyCode)
        => $"{currencyCode} {amount:N2}";

    private static string Escape(string value)
        => WebUtility.HtmlEncode(value);
}
