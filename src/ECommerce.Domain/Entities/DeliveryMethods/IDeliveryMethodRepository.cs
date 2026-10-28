namespace ECommerce.Domain.Entities.DeliveryMethods
{
    public interface IDeliveryMethodRepository
    {
        Task<DeliveryMethod?> GetByIdAsync(DeliveryMethodId id, CancellationToken ct = default);
        void Add(DeliveryMethod deliveryMethod);
        void Remove(DeliveryMethod deliveryMethod);
    }
}
