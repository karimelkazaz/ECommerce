using ECommerce.Domain.Common;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities.Orders;

/// <summary>
/// Read-side projection for exactly-once revenue reporting: one row per UTC
/// paid-order day per currency. <see cref="OrderPaidDomainEvent"/>'s consumer
/// upserts the row in the same transaction that claims the event, so the inbox
/// claim and the revenue accrual stay atomic.
/// </summary>
public sealed class SalesSummary : Entity<Guid>
{
    public DateOnly Day { get; private set; }
    public string CurrencyCode { get; private set; } = null!;
    public decimal Revenue { get; private set; }
    public int OrderCount { get; private set; }

    private SalesSummary() { }

    private SalesSummary(Guid id, DateOnly day, Currency currency, Money total)
        : base(id)
    {
        Day = day;
        CurrencyCode = currency.Code;
        Revenue = total.Amount;
        OrderCount = 1;
    }

    public static Result<SalesSummary> Start(DateOnly day, Money total)
    {
        if (total.Currency == Currency.None)
            return Result.Failure<SalesSummary>(SalesSummaryErrors.InvalidCurrency);

        if (total.Amount < 0)
            return Result.Failure<SalesSummary>(SalesSummaryErrors.InvalidAmount);

        return Result.Success(new SalesSummary(Guid.NewGuid(), day, total.Currency, total));
    }

    public void ApplySale(Money total)
    {
        if (total.Currency.Code != CurrencyCode)
            throw new InvalidOperationException(
                $"Cannot accrue {total.Currency.Code} into a {CurrencyCode} sales summary.");

        Revenue += total.Amount;
        OrderCount += 1;
    }
}

public static class SalesSummaryErrors
{
    public static readonly Error InvalidCurrency = Error.Validation("SalesSummary.InvalidCurrency", "Currency is invalid.");
    public static readonly Error InvalidAmount = Error.Validation("SalesSummary.InvalidAmount", "Amount cannot be negative.");
    public static readonly Error CurrencyMismatch = Error.Conflict("SalesSummary.CurrencyMismatch", "The sale cannot be accrued into this summary's currency.");
}
