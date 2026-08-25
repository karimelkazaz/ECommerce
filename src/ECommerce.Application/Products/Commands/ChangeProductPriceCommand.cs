using ECommerce.Application.Common;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.Products.Commands;

public sealed record ChangeProductPriceCommand(
    Guid Id,
    decimal Amount,
    string CurrencyCode) : IRequest<Result>;

public sealed class ChangeProductPriceCommandHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork) : IRequestHandler<ChangeProductPriceCommand, Result>
{
    public async Task<Result> Handle(ChangeProductPriceCommand request, CancellationToken cancellationToken)
    {
        if (!CurrencyResolver.TryResolve(request.CurrencyCode, out var currencyError, out var currency))
            return Result.Failure(currencyError);

        var moneyResult = Money.Create(request.Amount, currency);
        if (moneyResult.IsFailure)
            return Result.Failure(moneyResult.Error);

        var product = await products.GetByIdAsync(new ProductId(request.Id), cancellationToken);
        if (product is null)
            return Result.Failure(ProductErrors.NotFound);

        var result = product.ChangePrice(moneyResult.Value);
        if (result.IsFailure)
            return Result.Failure(result.Error);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class ChangeProductPriceCommandValidator : AbstractValidator<ChangeProductPriceCommand>
{
    public ChangeProductPriceCommandValidator()
    {
        RuleFor(c => c.Id)
            .NotEqual(Guid.Empty);

        RuleFor(c => c.Amount)
            .GreaterThan(0);

        RuleFor(c => c.CurrencyCode)
            .NotEmpty()
            .Must(code => CurrencyResolver.TryResolve(code, out _, out _))
                .WithMessage("Currency code must be one of: EGP, USD, EUR.");
    }
}
