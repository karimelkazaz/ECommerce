using ECommerce.Domain.Entities.Orders;

namespace ECommerce.Domain.Entities.Orders
{
    public interface IOrderRepository
    {
        Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default);
        Task<Order?> GetByPaymentIntentIdAsync(string paymentIntentId, CancellationToken ct = default);
        Task<IReadOnlyList<Order>> GetPendingByUserAsync(Guid userId, CancellationToken ct = default);
        void Add(Order order);
    }
}
