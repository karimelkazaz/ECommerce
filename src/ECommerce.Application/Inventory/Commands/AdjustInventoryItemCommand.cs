using ECommerce.Application.Common;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Inventory;
using ECommerce.Domain.Entities.Products;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.Inventory.Commands;

public sealed record AdjustInventoryItemCommand(Guid ProductId, int Delta) : IRequest<Result>;

public sealed class AdjustInventoryItemCommandHandler(
    IInventoryItemRepository inventory,
    IUnitOfWork unitOfWork) : IRequestHandler<AdjustInventoryItemCommand, Result>
{
    public async Task<Result> Handle(AdjustInventoryItemCommand request, CancellationToken cancellationToken)
    {
        var productId = new ProductId(request.ProductId);

        var inventoryItem = await inventory.GetByProductIdAsync(productId, cancellationToken);
        if (inventoryItem is null)
            return Result.Failure(InventoryErrors.NotFound);

        var adjustResult = inventoryItem.Adjust(request.Delta);
        if (adjustResult.IsFailure)
            return adjustResult;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed class AdjustInventoryItemCommandValidator : AbstractValidator<AdjustInventoryItemCommand>
{
    public AdjustInventoryItemCommandValidator()
    {
        RuleFor(c => c.ProductId)
            .NotEqual(Guid.Empty);

        RuleFor(c => c.Delta)
            .NotEqual(0);
    }
}
