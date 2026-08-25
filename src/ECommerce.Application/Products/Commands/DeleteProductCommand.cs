using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Products;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.Products.Commands;

public sealed record DeleteProductCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteProductCommandHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteProductCommand, Result>
{
    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(new ProductId(request.Id), cancellationToken);
        if (product is null)
            return Result.Failure(ProductErrors.NotFound);

        products.Remove(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed class DeleteProductCommandValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductCommandValidator()
    {
        RuleFor(c => c.Id)
            .NotEqual(Guid.Empty);
    }
}
