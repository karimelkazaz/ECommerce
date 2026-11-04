using ECommerce.Application.Common;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Inventory;
using ECommerce.Domain.Entities.Products;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.Inventory.Commands;

public sealed record CreateInventoryItemCommand(Guid ProductId, int Quantity) : IRequest<Result<InventoryItemResponse>>;

public sealed class CreateInventoryItemCommandHandler(
    IProductRepository products,
    IInventoryItemRepository inventory,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateInventoryItemCommand, Result<InventoryItemResponse>>
{
    public async Task<Result<InventoryItemResponse>> Handle(CreateInventoryItemCommand request, CancellationToken cancellationToken)
    {
        var productId = new ProductId(request.ProductId);

        var product = await products.GetByIdAsync(productId, cancellationToken);
        if (product is null)
            return Result.Failure<InventoryItemResponse>(ProductErrors.NotFound);

        if (await inventory.GetByProductIdAsync(productId, cancellationToken) is not null)
            return Result.Failure<InventoryItemResponse>(InventoryErrors.Duplicate);

        var createResult = InventoryItem.Create(productId, request.Quantity);
        if (createResult.IsFailure)
            return Result.Failure<InventoryItemResponse>(createResult.Error);

        inventory.Add(createResult.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new InventoryItemResponse(productId.Value, createResult.Value.AvailableQuantity);

        return Result.Success(response);
    }
}

public sealed class CreateInventoryItemCommandValidator : AbstractValidator<CreateInventoryItemCommand>
{
    public CreateInventoryItemCommandValidator()
    {
        RuleFor(c => c.ProductId)
            .NotEqual(Guid.Empty);

        RuleFor(c => c.Quantity)
            .GreaterThanOrEqualTo(0);
    }
}
