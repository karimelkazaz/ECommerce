using ECommerce.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Email;

public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct)
    {
        logger.LogInformation(
            "E-mail would be sent to {To}. Subject: {Subject}. Body: {Body}",
            to,
            subject,
            htmlBody);
        return Task.CompletedTask;
    }
}