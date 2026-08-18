using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.Entities.ProductsType.Events;

namespace ECommerce.Domain.Entities.ProductsType
{
    public class ProductType : Entity<ProductTypeId>
    {
        public const int MaxNameLength = 200;

        public string Name { get; private set; } = null!;

        public ICollection<Product> Products { get; private set; } = [];

        private ProductType()
        {
        }

        private ProductType(ProductTypeId id, string name): base(id)
        {
            Name = name;
        }

        public static Result<ProductType> Create(Guid id, string name)
        {
            if (id == Guid.Empty)
                return Result.Failure<ProductType>(ProductTypeErrors.InvalidId);

            if (string.IsNullOrWhiteSpace(name))
                return Result.Failure<ProductType>(ProductTypeErrors.InvalidName);

            if (name.Length > MaxNameLength)
                return Result.Failure<ProductType>(ProductTypeErrors.NameTooLong);

            var productType = new ProductType(new ProductTypeId(id), name.Trim());
            productType.RaiseDomainEvent(new ProductTypeCreatedDomainEvent(productType.Id));

            return Result.Success(productType);
        }

        public Result Rename(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Result.Failure(ProductTypeErrors.InvalidName);

            if (name.Length > MaxNameLength)
                return Result.Failure(ProductTypeErrors.NameTooLong);

            Name = name.Trim();
            RaiseDomainEvent(new ProductTypeRenamedDomainEvent(Id));
            return Result.Success();
        }
    }
}