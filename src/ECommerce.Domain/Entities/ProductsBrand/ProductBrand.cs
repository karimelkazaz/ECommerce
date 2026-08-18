using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.Entities.ProductsBrand.Events;

namespace ECommerce.Domain.Entities.ProductsBrand
{
    public class ProductBrand : Entity<ProductBrandId>
    {
        public const int MaxNameLength = 200;

        public string Name { get; private set; } = null!;

        public ICollection<Product> Products { get; private set; } = [];

        private ProductBrand()
        {
        }

        private ProductBrand(ProductBrandId id, string name) : base(id)
        {
            Name = name;
        }

        public static Result<ProductBrand> Create(Guid id, string name)
        {
            if (id == Guid.Empty)
                return Result.Failure<ProductBrand>(ProductBrandErrors.InvalidId);

            if (string.IsNullOrWhiteSpace(name))
                return Result.Failure<ProductBrand>(ProductBrandErrors.InvalidName);

            if (name.Length > MaxNameLength)
                return Result.Failure<ProductBrand>(ProductBrandErrors.NameTooLong);

            var productBrand = new ProductBrand(new ProductBrandId(id), name.Trim());
            productBrand.RaiseDomainEvent(new ProductBrandCreatedDomainEvent(productBrand.Id));

            return Result.Success(productBrand);
        }

        public Result Rename(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Result.Failure(ProductBrandErrors.InvalidName);

            if (name.Length > MaxNameLength)
                return Result.Failure(ProductErrors.NameTooLong);

            Name = name.Trim();
            RaiseDomainEvent(new ProductBrandRenamedDomainEvent(Id));
            return Result.Success();
        }
    }
}