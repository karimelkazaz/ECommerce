using ECommerce.Application.Common;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.DeliveryMethods;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.DeliveryMethods;
using ECommerce.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.DeliveryMethods.Commands;

public sealed record CreateDeliveryMethodCommand(
    string Name,
    string DeliveryTime,
    decimal Price,
    string CurrencyCode,
    string? Description = null) : IRequest<Result<DeliveryMethodResponse>>;

public sealed class CreateDeliveryMethodCommandHandler(
    IDeliveryMethodRepository deliveryMethods,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateDeliveryMethodCommand, Result<DeliveryMethodResponse>>
{
    public async Task<Result<DeliveryMethodResponse>> Handle(CreateDeliveryMethodCommand request, CancellationToken cancellationToken)
    {
        if (!CurrencyResolver.TryResolve(request.CurrencyCode, out var currencyError, out var currency))
            return Result.Failure<DeliveryMethodResponse>(currencyError);

        var priceResult = Money.Create(request.Price, currency);
        if (priceResult.IsFailure)
            return Result.Failure<DeliveryMethodResponse>(priceResult.Error);

        var result = DeliveryMethod.Create(
            DeliveryMethodId.New(),
            request.Name,
            request.DeliveryTime,
            priceResult.Value,
            request.Description);

        if (result.IsFailure)
            return Result.Failure<DeliveryMethodResponse>(result.Error);

        var deliveryMethod = result.Value;
        deliveryMethods.Add(deliveryMethod);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new DeliveryMethodResponse(
            deliveryMethod.Id.Value,
            deliveryMethod.Name,
            deliveryMethod.DeliveryTime,
            deliveryMethod.Price.Amount,
            deliveryMethod.Price.Currency.Code,
            deliveryMethod.Description,
            deliveryMethod.IsAvailable);
    }
}

public sealed class CreateDeliveryMethodCommandValidator : AbstractValidator<CreateDeliveryMethodCommand>
{
    public CreateDeliveryMethodCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
            .MaximumLength(DeliveryMethod.MaxNameLength);

        RuleFor(c => c.DeliveryTime)
            .NotEmpty()
            .MaximumLength(DeliveryMethod.MaxDeliveryTimeLength);

        RuleFor(c => c.Description)
            .MaximumLength(DeliveryMethod.MaxDescriptionLength);

        RuleFor(c => c.Price)
            .GreaterThanOrEqualTo(0);

        RuleFor(c => c.CurrencyCode)
            .NotEmpty()
            .Must(code => CurrencyResolver.TryResolve(code, out _, out _))
                .WithMessage("Currency code must be one of: EGP, USD, EUR.");
    }
}
