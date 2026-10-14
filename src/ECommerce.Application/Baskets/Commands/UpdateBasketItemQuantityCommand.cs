using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Baskets;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.Baskets.Commands;

public sealed record UpdateBasketItemQuantityCommand(
    Guid ProductId,
    int Quantity,
    Guid? UserId = null) : IRequest<Result<BasketResponse>>;

public sealed class UpdateBasketItemQuantityCommandHandler(
    IBasketStore basketStore,
    ICurrentUserService currentUserService) : IRequestHandler<UpdateBasketItemQuantityCommand, Result<BasketResponse>>
{
    public async Task<Result<BasketResponse>> Handle(UpdateBasketItemQuantityCommand request, CancellationToken cancellationToken)
    {
        var userId = request.UserId ?? currentUserService.UserId;
        if (userId is null || userId == Guid.Empty)
            return Result.Failure<BasketResponse>(BasketErrors.InvalidUserId);

        var basket = await basketStore.GetAsync(userId.Value, cancellationToken);
        if (basket is null)
            return Result.Failure<BasketResponse>(BasketErrors.NotFound);

        var updateResult = basket.UpdateQuantity(new ProductId(request.ProductId), request.Quantity);
        if (updateResult.IsFailure)
            return Result.Failure<BasketResponse>(updateResult.Error);

        await basketStore.SaveAsync(basket, cancellationToken);

        return BasketResponse.FromDomain(basket);
    }
}

public sealed class UpdateBasketItemQuantityCommandValidator : AbstractValidator<UpdateBasketItemQuantityCommand>
{
    public UpdateBasketItemQuantityCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be at least 1.");
    }
}

