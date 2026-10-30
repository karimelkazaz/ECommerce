using ECommerce.Domain.Entities.Orders;
using ECommerce.Infrastructure.Persistence.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository(ApplicationDbContext dbContext) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default)
        => dbContext.OrdersSet.FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<Order?> GetByPaymentIntentIdAsync(string paymentIntentId, CancellationToken ct = default)
        => dbContext.OrdersSet.FirstOrDefaultAsync(o => o.PaymentIntentId == paymentIntentId, ct);

    public async Task<IReadOnlyList<Order>> GetPendingByUserAsync(Guid userId, CancellationToken ct = default)
        => await dbContext.OrdersSet
            .Where(o => o.UserId == userId && o.Status == OrderStatus.Pending)
            .ToListAsync(ct);

    public void Add(Order order)
        => dbContext.OrdersSet.Add(order);
}
