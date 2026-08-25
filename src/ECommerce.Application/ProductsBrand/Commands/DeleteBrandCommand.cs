using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.ProductsBrand;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.ProductsBrand.Commands;

public sealed record DeleteBrandCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteBrandCommandHandler(
    IProductBrandRepository brands,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteBrandCommand, Result>
{
    public async Task<Result> Handle(DeleteBrandCommand request, CancellationToken cancellationToken)
    {
        var brand = await brands.GetByIdAsync(new ProductBrandId(request.Id), cancellationToken);
        if (brand is null)
            return Result.Failure(ProductBrandErrors.NotFound);

        brands.Remove(brand);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed class DeleteBrandCommandValidator : AbstractValidator<DeleteBrandCommand>
{
    public DeleteBrandCommandValidator()
    {
        RuleFor(c => c.Id)
            .NotEqual(Guid.Empty);
    }
}
