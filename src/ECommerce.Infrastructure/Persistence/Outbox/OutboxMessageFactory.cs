using System.Text.Json;

namespace ECommerce.Infrastructure.Persistence.Outbox;

internal static class OutboxMessageFactory
{
    public static OutboxMessage Create(object message, DateTimeOffset occurredOnUtc)
    {
        ArgumentNullException.ThrowIfNull(message);

        var type = message.GetType();

        return OutboxMessage.Create(
            type.AssemblyQualifiedName ?? type.FullName ?? type.Name,
            JsonSerializer.Serialize(message, type, OutboxJsonOptions.Default),
            occurredOnUtc);
    }
}
