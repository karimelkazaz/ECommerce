namespace ECommerce.Domain.Entities.Products
{
    public interface IProductRepository
    {
        Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default);
        void Add(Product product);
    }
}