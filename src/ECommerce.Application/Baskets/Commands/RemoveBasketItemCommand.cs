using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Baskets;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.Baskets.Commands;

public sealed record RemoveBasketItemCommand(
    Guid ProductId,
    Guid? UserId = null) : IRequest<Result<BasketResponse>>;

public sealed class RemoveBasketItemCommandHandler(
    IBasketStore basketStore,
    ICurrentUserService currentUserService) : IRequestHandler<RemoveBasketItemCommand, Result<BasketResponse>>
{
    public async Task<Result<BasketResponse>> Handle(RemoveBasketItemCommand request, CancellationToken cancellationToken)
    {
        var userId = request.UserId ?? currentUserService.UserId;
        if (userId is null || userId == Guid.Empty)
            return Result.Failure<BasketResponse>(BasketErrors.InvalidUserId);

        var basket = await basketStore.GetAsync(userId.Value, cancellationToken);
        if (basket is null)
            return Result.Failure<BasketResponse>(BasketErrors.NotFound);

        var removeResult = basket.RemoveItem(new ProductId(request.ProductId));
        if (removeResult.IsFailure)
            return Result.Failure<BasketResponse>(removeResult.Error);

        await basketStore.SaveAsync(basket, cancellationToken);

        return BasketResponse.FromDomain(basket);
    }
}

public sealed class RemoveBasketItemCommandValidator : AbstractValidator<RemoveBasketItemCommand>
{
    public RemoveBasketItemCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required.");
    }
}

