using ECommerce.Application.Common;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.DeliveryMethods;
using ECommerce.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.DeliveryMethods.Commands;

public sealed record ChangeDeliveryMethodPriceCommand(
    Guid Id,
    decimal Amount,
    string CurrencyCode) : IRequest<Result>;

public sealed class ChangeDeliveryMethodPriceCommandHandler(
    IDeliveryMethodRepository deliveryMethods,
    IUnitOfWork unitOfWork) : IRequestHandler<ChangeDeliveryMethodPriceCommand, Result>
{
    public async Task<Result> Handle(ChangeDeliveryMethodPriceCommand request, CancellationToken cancellationToken)
    {
        if (!CurrencyResolver.TryResolve(request.CurrencyCode, out var currencyError, out var currency))
            return Result.Failure(currencyError);

        var moneyResult = Money.Create(request.Amount, currency);
        if (moneyResult.IsFailure)
            return Result.Failure(moneyResult.Error);

        var deliveryMethod = await deliveryMethods.GetByIdAsync(new DeliveryMethodId(request.Id), cancellationToken);
        if (deliveryMethod is null)
            return Result.Failure(DeliveryMethodErrors.NotFound);

        var result = deliveryMethod.ChangePrice(moneyResult.Value);
        if (result.IsFailure)
            return Result.Failure(result.Error);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class ChangeDeliveryMethodPriceCommandValidator : AbstractValidator<ChangeDeliveryMethodPriceCommand>
{
    public ChangeDeliveryMethodPriceCommandValidator()
    {
        RuleFor(c => c.Id)
            .NotEqual(Guid.Empty);

        RuleFor(c => c.Amount)
            .GreaterThanOrEqualTo(0);

        RuleFor(c => c.CurrencyCode)
            .NotEmpty()
            .Must(code => CurrencyResolver.TryResolve(code, out _, out _))
                .WithMessage("Currency code must be one of: EGP, USD, EUR.");
    }
}
