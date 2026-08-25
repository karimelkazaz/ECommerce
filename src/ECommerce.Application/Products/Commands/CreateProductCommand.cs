using ECommerce.Application.Common;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.Entities.ProductsBrand;
using ECommerce.Domain.Entities.ProductsType;
using ECommerce.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.Products.Commands;

public sealed record CreateProductCommand(
    string Name,
    string Description,
    string PictureUrl,
    decimal Price,
    string CurrencyCode,
    Guid BrandId,
    Guid TypeId) : IRequest<Result<ProductResponse>>;

public sealed class CreateProductCommandHandler(
    IProductRepository products,
    IProductBrandRepository brands,
    IProductTypeRepository types,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateProductCommand, Result<ProductResponse>>
{
    public async Task<Result<ProductResponse>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        if (!CurrencyResolver.TryResolve(request.CurrencyCode, out var currencyError, out var currency))
            return Result.Failure<ProductResponse>(currencyError);

        var priceResult = Money.Create(request.Price, currency);
        if (priceResult.IsFailure)
            return Result.Failure<ProductResponse>(priceResult.Error);

        var brandId = new ProductBrandId(request.BrandId);
        var typeId = new ProductTypeId(request.TypeId);

        var brand = await brands.GetByIdAsync(brandId, cancellationToken);
        if (brand is null)
            return Result.Failure<ProductResponse>(ProductBrandErrors.NotFound);

        var type = await types.GetByIdAsync(typeId, cancellationToken);
        if (type is null)
            return Result.Failure<ProductResponse>(ProductTypeErrors.NotFound);

        var result = Product.Create(ProductId.New(), request.Name, request.Description, request.PictureUrl, priceResult.Value);
        if (result.IsFailure)
            return Result.Failure<ProductResponse>(result.Error);

        var product = result.Value;

        var brandResult = product.ChangeBrand(brandId);
        if (brandResult.IsFailure)
            return Result.Failure<ProductResponse>(brandResult.Error);

        var typeResult = product.ChangeType(typeId);
        if (typeResult.IsFailure)
            return Result.Failure<ProductResponse>(typeResult.Error);

        products.Add(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ProductResponse(
            product.Id.Value,
            product.Name,
            product.Description,
            product.PictureUrl,
            product.Price.Amount,
            product.Price.Currency.Code,
            brand.Name,
            type.Name);
    }
}

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
            .MaximumLength(Product.MaxNameLength);

        RuleFor(c => c.Description)
            .NotEmpty()
            .MaximumLength(Product.MaxDescriptionLength);

        RuleFor(c => c.PictureUrl)
            .NotEmpty()
            .MaximumLength(Product.MaxPictureUrlLength);

        RuleFor(c => c.Price)
            .GreaterThan(0);

        RuleFor(c => c.CurrencyCode)
            .NotEmpty()
            .Must(code => CurrencyResolver.TryResolve(code, out _, out _))
                .WithMessage("Currency code must be one of: EGP, USD, EUR.");

        RuleFor(c => c.BrandId)
            .NotEqual(Guid.Empty);

        RuleFor(c => c.TypeId)
            .NotEqual(Guid.Empty);
    }
}
