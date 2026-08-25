using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.ProductsType;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.ProductsType.Commands;

public sealed record RenameTypeCommand(Guid Id, string Name) : IRequest<Result>;

public sealed class RenameTypeCommandHandler(
    IProductTypeRepository types,
    IUnitOfWork unitOfWork) : IRequestHandler<RenameTypeCommand, Result>
{
    public async Task<Result> Handle(RenameTypeCommand request, CancellationToken cancellationToken)
    {
        var type = await types.GetByIdAsync(new ProductTypeId(request.Id), cancellationToken);
        if (type is null)
            return Result.Failure(ProductTypeErrors.NotFound);

        var result = type.Rename(request.Name);
        if (result.IsFailure)
            return Result.Failure(result.Error);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class RenameTypeCommandValidator : AbstractValidator<RenameTypeCommand>
{
    public RenameTypeCommandValidator()
    {
        RuleFor(c => c.Id)
            .NotEqual(Guid.Empty);

        RuleFor(c => c.Name)
            .NotEmpty()
            .MaximumLength(ProductType.MaxNameLength);
    }
}
