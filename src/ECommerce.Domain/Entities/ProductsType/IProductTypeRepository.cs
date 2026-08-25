namespace ECommerce.Domain.Entities.ProductsType;

public interface IProductTypeRepository
{
    Task<ProductType?> GetByIdAsync(ProductTypeId id, CancellationToken ct = default);
    void Add(ProductType type);
    void Remove(ProductType type);
}
