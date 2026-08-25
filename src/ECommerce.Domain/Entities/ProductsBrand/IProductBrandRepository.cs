namespace ECommerce.Domain.Entities.ProductsBrand
{
    public interface IProductBrandRepository
    {
        Task<ProductBrand?> GetByIdAsync(ProductBrandId id, CancellationToken ct = default);
        void Add(ProductBrand brand);
        void Remove(ProductBrand brand);
    }
}
