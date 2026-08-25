using ECommerce.Domain.ValueObjects;
using Xunit;

namespace ECommerce.Domain.UnitTests;

public class MoneyTests
{
    [Fact]
    public void Create_WithNegativeAmount_Fails()
    {
        var result = Money.Create(-1m, Currency.Usd);

        Assert.True(result.IsFailure);
        Assert.Equal("Money.NegativeAmount", result.Error.Code);
    }

    [Fact]
    public void Create_WithZeroAmount_Succeeds()
    {
        var result = Money.Create(0m, Currency.Usd);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value.Amount);
    }

    [Fact]
    public void Add_WithSameCurrency_ReturnsSum()
    {
        var left = Money.Create(100.50m, Currency.Eur).Value;
        var right = Money.Create(49.50m, Currency.Eur).Value;

        var sum = left.Add(right);

        Assert.Equal(150m, sum.Amount);
        Assert.Equal(Currency.Eur, sum.Currency);
    }

    [Fact]
    public void Add_WithDifferentCurrency_Throws()
    {
        var usd = Money.Create(100m, Currency.Usd).Value;
        var eur = Money.Create(100m, Currency.Eur).Value;

        Assert.Throws<InvalidOperationException>(() => usd.Add(eur));
    }

    [Fact]
    public void SameAmountAndCurrency_AreEqual()
    {
        var a = Money.Create(10m, Currency.Egp).Value;
        var b = Money.Create(10m, Currency.Egp).Value;

        Assert.Equal(a, b);
    }
}
