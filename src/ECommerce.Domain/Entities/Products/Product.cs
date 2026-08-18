using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Products.Events;
using ECommerce.Domain.Entities.ProductsBrand;
using ECommerce.Domain.Entities.ProductsType;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities.Products;

public class Product : Entity<ProductId>
{
    public const int MaxNameLength = 100;
    public const int MaxDescriptionLength = 1000;
    public const int MaxPictureUrlLength = 500;
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string PictureUrl { get; private set; } = null!;
    public Money Price { get; private set; } = null!;

    public ProductBrandId ProductBrandId { get; private set; }
    public ProductBrand ProductBrand { get; private set; } = null!;

    public ProductTypeId ProductTypeId { get; private set; }
    public ProductType ProductType { get; private set; } = null!;

    private Product() { }

    private Product(
        ProductId id,
        string name,
        string description,
        string pictureUrl,
        Money price)
        : base(id)
    {
        Name = name;
        Description = description;
        PictureUrl = pictureUrl;
        Price = price;
    }

    public static Result<Product> Create(ProductId id, string name, string description, string pictureUrl, Money price)
    {
        if (id == default)
            return Result.Failure<Product>(ProductErrors.InvalidId);

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Product>(ProductErrors.NameRequired);

        if (name.Length > MaxNameLength)
            return Result.Failure<Product>(ProductErrors.NameTooLong);

        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure<Product>(ProductErrors.DescriptionRequired);

        if (description.Length > MaxDescriptionLength)
            return Result.Failure<Product>(ProductErrors.DescriptionTooLong);

        if (string.IsNullOrWhiteSpace(pictureUrl))
            return Result.Failure<Product>(ProductErrors.PictureUrlRequired);

        if (pictureUrl.Length > MaxPictureUrlLength)
            return Result.Failure<Product>(ProductErrors.PictureUrlTooLong);

        if (price <= 0)
            return Result.Failure<Product>(ProductErrors.InvalidPrice);

        var product = new Product(ProductId.New(), name, description, pictureUrl, price);
        product.RaiseDomainEvent(new ProductCreatedDomainEvent(product.Id));
        return Result.Success(product);
    }

    public Result Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(ProductErrors.NameRequired);

        if (name.Length > MaxNameLength)
            return Result.Failure(ProductErrors.NameTooLong);

        Name = name.Trim();
        RaiseDomainEvent(new ProductRenamedDomainEvent(Id, Name));

        return Result.Success();
    }

    public Result ChangeDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure(ProductErrors.DescriptionRequired);

        if (description.Length > MaxDescriptionLength)
            return Result.Failure(ProductErrors.DescriptionTooLong);

        Description = description.Trim();
        RaiseDomainEvent(new ProductDescriptionChangedDomainEvent(Id, Description));

        return Result.Success();
    }

    public Result ChangePictureUrl(string pictureUrl)
    {
        if (string.IsNullOrWhiteSpace(pictureUrl))
            return Result.Failure(ProductErrors.PictureUrlRequired);

        if (pictureUrl.Length > MaxPictureUrlLength)
            return Result.Failure(ProductErrors.PictureUrlTooLong);

        PictureUrl = pictureUrl.Trim();
        RaiseDomainEvent(new ProductPictureUrlChangedDomainEvent(Id, PictureUrl));

        return Result.Success();
    }

    public Result ChangePrice(Money price)
    {
        if (price <= 0)
            return Result.Failure(ProductErrors.InvalidPrice);

        var oldPrice = Price;
        Price = price;
        RaiseDomainEvent(new ProductPriceChangedDomainEvent(Id, oldPrice, price));

        return Result.Success();
    }

    public Result ChangeBrand(ProductBrandId productBrandId)
    {
        if (productBrandId.Value == Guid.Empty)
            return Result.Failure(ProductErrors.InvalidBrand);

        ProductBrandId = productBrandId;
        RaiseDomainEvent(new ProductBrandChangedDomainEvent(Id, productBrandId));

        return Result.Success();
    }

    public Result ChangeType(ProductTypeId productTypeId)
    {
        if (productTypeId.Value == Guid.Empty)
            return Result.Failure(ProductErrors.InvalidType);

        ProductTypeId = productTypeId;
        RaiseDomainEvent(new ProductTypeChangedDomainEvent(Id, productTypeId));

        return Result.Success();
    }
}
