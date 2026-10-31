using ECommerce.Application.Common.Interfaces;
using Microsoft.Extensions.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Email;

public sealed class SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct)
    {
        var smtp = options.Value;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(smtp.FromName, smtp.FromEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(
            smtp.Host,
            smtp.Port,
            smtp.EnableSsl ? SecureSocketOptions.StartTlsWhenAvailable : SecureSocketOptions.None,
            ct);

        if (!string.IsNullOrEmpty(smtp.Username))
        {
            await client.AuthenticateAsync(smtp.Username, smtp.Password ?? string.Empty, ct);
        }

        await client.SendAsync(message, ct);
        logger.LogInformation(
    "Email sent successfully to {Recipient}. Subject: {Subject}",
    to,
    subject);
        await client.DisconnectAsync(true, ct);
    }
}