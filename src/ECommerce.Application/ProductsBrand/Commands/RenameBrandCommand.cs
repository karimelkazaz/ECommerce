using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.ProductsBrand;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.ProductsBrand.Commands;

public sealed record RenameBrandCommand(Guid Id, string Name) : IRequest<Result>;

public sealed class RenameBrandCommandHandler(
    IProductBrandRepository brands,
    IUnitOfWork unitOfWork) : IRequestHandler<RenameBrandCommand, Result>
{
    public async Task<Result> Handle(RenameBrandCommand request, CancellationToken cancellationToken)
    {
        var brand = await brands.GetByIdAsync(new ProductBrandId(request.Id), cancellationToken);
        if (brand is null)
            return Result.Failure(ProductBrandErrors.NotFound);

        var result = brand.Rename(request.Name);
        if (result.IsFailure)
            return Result.Failure(result.Error);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class RenameBrandCommandValidator : AbstractValidator<RenameBrandCommand>
{
    public RenameBrandCommandValidator()
    {
        RuleFor(c => c.Id)
            .NotEqual(Guid.Empty);

        RuleFor(c => c.Name)
            .NotEmpty()
            .MaximumLength(ProductBrand.MaxNameLength);
    }
}
