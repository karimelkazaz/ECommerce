using ECommerce.Application.Common.Interfaces;
using ECommerce.Infrastructure.Persistence.DbContexts;
using MediatR;

namespace ECommerce.Infrastructure.Persistence.Outbox;

public sealed class OutboxWriter(ApplicationDbContext dbContext, TimeProvider timeProvider) : IOutboxWriter
{
    public void Enqueue(INotification message)
    {
        var outboxMessage = OutboxMessageFactory.Create(message, timeProvider.GetUtcNow());

        dbContext.OutboxMessages.Add(outboxMessage);
    }
}
