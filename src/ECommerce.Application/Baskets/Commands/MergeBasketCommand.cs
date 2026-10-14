using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Baskets;
using ECommerce.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.Baskets.Commands;

public sealed record MergeBasketCommand(
    Guid SourceUserId,
    Guid? DestinationUserId = null) : IRequest<Result<BasketResponse>>;

public sealed class MergeBasketCommandHandler(
    IBasketStore basketStore,
    ICurrentUserService currentUserService) : IRequestHandler<MergeBasketCommand, Result<BasketResponse>>
{
    public async Task<Result<BasketResponse>> Handle(MergeBasketCommand request, CancellationToken cancellationToken)
    {
        var destinationUserId = request.DestinationUserId ?? currentUserService.UserId;
        if (destinationUserId is null || destinationUserId == Guid.Empty)
            return Result.Failure<BasketResponse>(BasketErrors.InvalidUserId);

        if (request.SourceUserId == Guid.Empty)
            return Result.Failure<BasketResponse>(BasketErrors.MergeSourceRequired);

        if (request.SourceUserId == destinationUserId.Value)
            return Result.Failure<BasketResponse>(BasketErrors.CannotMergeSelf);

        var sourceBasket = await basketStore.GetAsync(request.SourceUserId, cancellationToken);
        var destinationBasket = await basketStore.GetOrCreateAsync(destinationUserId.Value, cancellationToken);

        if (sourceBasket is not null && sourceBasket.Items.Count > 0)
        {
            var mergeResult = destinationBasket.MergeFrom(sourceBasket);
            if (mergeResult.IsFailure)
                return Result.Failure<BasketResponse>(mergeResult.Error);

            await basketStore.SaveAsync(destinationBasket, cancellationToken);
            await basketStore.DeleteAsync(request.SourceUserId, cancellationToken);
        }

        return BasketResponse.FromDomain(destinationBasket);
    }
}

public sealed class MergeBasketCommandValidator : AbstractValidator<MergeBasketCommand>
{
    public MergeBasketCommandValidator()
    {
        RuleFor(x => x.SourceUserId)
            .NotEmpty()
            .WithMessage("Source user ID is required.");
    }
}

