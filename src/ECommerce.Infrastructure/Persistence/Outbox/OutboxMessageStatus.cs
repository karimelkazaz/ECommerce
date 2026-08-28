namespace ECommerce.Infrastructure.Persistence.Outbox;

public enum OutboxMessageStatus
{
    Pending = 0,
    Processed = 1,
    Failed = 2,
}
