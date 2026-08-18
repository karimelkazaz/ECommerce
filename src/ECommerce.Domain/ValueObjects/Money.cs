using ECommerce.Domain.Common;

namespace ECommerce.Domain.ValueObjects;
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public Currency Currency { get; }

    private Money(decimal amount, Currency currency) => (Amount, Currency) = (amount, currency);

    public static Result<Money> Create(decimal amount, Currency currency)
    {
        if (amount < 0) return Result.Failure<Money>(MoneyErrors.NegativeAmount);
        if (currency == Currency.None)
            return Result.Failure<Money>(MoneyErrors.InvalidCurrency);

        return new Money(amount, currency);
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot add {other.Currency} to {Currency}.");
        return new Money(Amount + other.Amount, Currency);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public static bool operator <=(Money m, int value)
    {
        return m.Amount <= value;
    }

    public static bool operator >=(Money m, int value)
    {
        return m.Amount >= value;
    }
}

public static class MoneyErrors
{
    public static readonly Error NegativeAmount = Error.Validation("Money.NegativeAmount", "Amount cannot be negative.");
    public static readonly Error InvalidCurrency = Error.Validation("Money.InvalidCurrency", "Currency is invalid.");
}