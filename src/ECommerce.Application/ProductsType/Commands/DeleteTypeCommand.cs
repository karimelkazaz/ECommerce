using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.ProductsType;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.ProductsType.Commands;

public sealed record DeleteTypeCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteTypeCommandHandler(
    IProductTypeRepository types,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteTypeCommand, Result>
{
    public async Task<Result> Handle(DeleteTypeCommand request, CancellationToken cancellationToken)
    {
        var type = await types.GetByIdAsync(new ProductTypeId(request.Id), cancellationToken);
        if (type is null)
            return Result.Failure(ProductTypeErrors.NotFound);

        types.Remove(type);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed class DeleteTypeCommandValidator : AbstractValidator<DeleteTypeCommand>
{
    public DeleteTypeCommandValidator()
    {
        RuleFor(c => c.Id)
            .NotEqual(Guid.Empty);
    }
}
