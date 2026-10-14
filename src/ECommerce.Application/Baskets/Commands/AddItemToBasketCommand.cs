using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Baskets;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.Baskets.Commands;

public sealed record AddItemToBasketCommand(
    Guid ProductId,
    int Quantity = 1,
    Guid? UserId = null) : IRequest<Result<BasketResponse>>;

public sealed class AddItemToBasketCommandHandler(
    IBasketStore basketStore,
    IProductRepository productRepository,
    ICurrentUserService currentUserService) : IRequestHandler<AddItemToBasketCommand, Result<BasketResponse>>
{
    public async Task<Result<BasketResponse>> Handle(AddItemToBasketCommand request, CancellationToken cancellationToken)
    {
        var userId = request.UserId ?? currentUserService.UserId;
        if (userId is null || userId == Guid.Empty)
            return Result.Failure<BasketResponse>(BasketErrors.InvalidUserId);

        var product = await productRepository.GetByIdAsync(new ProductId(request.ProductId), cancellationToken);
        if (product is null)
            return Result.Failure<BasketResponse>(ProductErrors.NotFound);

        var basket = await basketStore.GetOrCreateAsync(userId.Value, cancellationToken);

        var addResult = basket.AddItem(
            product.Id,
            product.Name,
            product.PictureUrl,
            product.Price,
            request.Quantity);

        if (addResult.IsFailure)
            return Result.Failure<BasketResponse>(addResult.Error);

        await basketStore.SaveAsync(basket, cancellationToken);

        return BasketResponse.FromDomain(basket);
    }
}

public sealed class AddItemToBasketCommandValidator : AbstractValidator<AddItemToBasketCommand>
{
    public AddItemToBasketCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be at least 1.");
    }
}

