using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Orders;
using ECommerce.Domain.ValueObjects;
using Xunit;

namespace ECommerce.Domain.UnitTests;

public class SalesSummaryTests
{
    private static readonly DateOnly Day = new(2026, 11, 7);

    private static Money Money(decimal amount, Currency? currency = null)
        => Domain.ValueObjects.Money.Create(amount, currency ?? Currency.Usd).Value;

    [Fact]
    public void Start_WithValidTotal_CreatesSummaryWithOneOrder()
    {
        var result = SalesSummary.Start(Day, Money(120.50m));

        Assert.True(result.IsSuccess);
        Assert.Equal(Day, result.Value.Day);
        Assert.Equal("USD", result.Value.CurrencyCode);
        Assert.Equal(120.50m, result.Value.Revenue);
        Assert.Equal(1, result.Value.OrderCount);
    }

    [Fact]
    public void ApplySale_AccruesRevenueAndOrderCount()
    {
        var summary = SalesSummary.Start(Day, Money(100m)).Value;

        summary.ApplySale(Money(45.25m));
        summary.ApplySale(Money(10.75m));

        Assert.Equal(156m, summary.Revenue);
        Assert.Equal(3, summary.OrderCount);
    }

    [Fact]
    public void ApplySale_WithWrongCurrency_Throws()
    {
        var summary = SalesSummary.Start(Day, Money(100m, Currency.Usd)).Value;

        Assert.Throws<InvalidOperationException>(
            () => summary.ApplySale(Money(10m, Currency.Eur)));
    }
}
