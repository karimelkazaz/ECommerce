using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.ProductsBrand;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.ProductsBrand;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.ProductsBrand.Commands;

public sealed record CreateBrandCommand(string Name) : IRequest<Result<BrandResponse>>;

public sealed class CreateBrandCommandHandler(
    IProductBrandRepository brands,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateBrandCommand, Result<BrandResponse>>
{
    public async Task<Result<BrandResponse>> Handle(CreateBrandCommand request, CancellationToken cancellationToken)
    {
        var result = ProductBrand.Create(ProductBrandId.New(), request.Name);
        if (result.IsFailure)
            return Result.Failure<BrandResponse>(result.Error);

        var brand = result.Value;
        brands.Add(brand);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new BrandResponse(brand.Id.Value, brand.Name);
    }
}

public sealed class CreateBrandCommandValidator : AbstractValidator<CreateBrandCommand>
{
    public CreateBrandCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
            .MaximumLength(ProductBrand.MaxNameLength);
    }
}
