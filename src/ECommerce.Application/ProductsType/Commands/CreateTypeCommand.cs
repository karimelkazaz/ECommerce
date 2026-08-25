using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.ProductsType;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.ProductsType;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.ProductsType.Commands;

public sealed record CreateTypeCommand(string Name) : IRequest<Result<TypeResponse>>;

public sealed class CreateTypeCommandHandler(
    IProductTypeRepository types,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateTypeCommand, Result<TypeResponse>>
{
    public async Task<Result<TypeResponse>> Handle(CreateTypeCommand request, CancellationToken cancellationToken)
    {
        var result = ProductType.Create(ProductTypeId.New(), request.Name);
        if (result.IsFailure)
            return Result.Failure<TypeResponse>(result.Error);

        var type = result.Value;
        types.Add(type);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new TypeResponse(type.Id.Value, type.Name);
    }
}

public sealed class CreateTypeCommandValidator : AbstractValidator<CreateTypeCommand>
{
    public CreateTypeCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
            .MaximumLength(ProductType.MaxNameLength);
    }
}
