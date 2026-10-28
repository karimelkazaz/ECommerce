using System.Text.Json;
using ECommerce.Domain.Entities.DeliveryMethods;
using ECommerce.Domain.Entities.DeliveryMethods.Events;
using ECommerce.Domain.ValueObjects;
using Xunit;

namespace ECommerce.Domain.UnitTests;

public class DeliveryMethodTests
{
    private static Money Price(decimal amount = 30m, Currency? currency = null)
        => Money.Create(amount, currency ?? Currency.Usd).Value;

    // Money.Create rejects negative amounts, so a negative price can only reach the
    // aggregate through the persistence layer, which materializes the value object
    // without going through the factory.
    private static Money NegativePrice()
        => JsonSerializer.Deserialize<Money>("""{"Amount":-1,"Currency":{"Code":"USD"}}""")!;

    private static DeliveryMethod Create(
        string name = "Standard Delivery",
        string deliveryTime = "3-5 business days",
        decimal price = 30m)
        => DeliveryMethod.Create(DeliveryMethodId.New(), name, deliveryTime, Price(price)).Value;

    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var result = DeliveryMethod.Create(DeliveryMethodId.New(), "Express", "1-2 days", Price(60m));

        Assert.True(result.IsSuccess);
        Assert.Equal("Express", result.Value.Name);
        Assert.Equal("1-2 days", result.Value.DeliveryTime);
        Assert.Equal(60m, result.Value.Price.Amount);
        Assert.Equal(Currency.Usd, result.Value.Price.Currency);
    }

    [Fact]
    public void Create_WithEmptyId_Fails()
    {
        var result = DeliveryMethod.Create(default, "Express", "1-2 days", Price());

        Assert.True(result.IsFailure);
        Assert.Equal(DeliveryMethodErrors.InvalidId.Code, result.Error.Code);
    }

    [Fact]
    public void Create_WithBlankName_Fails()
    {
        var result = DeliveryMethod.Create(DeliveryMethodId.New(), "  ", "1-2 days", Price());

        Assert.True(result.IsFailure);
        Assert.Equal(DeliveryMethodErrors.NameRequired.Code, result.Error.Code);
    }

    [Fact]
    public void Create_WithTooLongName_Fails()
    {
        var result = DeliveryMethod.Create(DeliveryMethodId.New(), new string('a', DeliveryMethod.MaxNameLength + 1), "1-2 days", Price());

        Assert.True(result.IsFailure);
        Assert.Equal(DeliveryMethodErrors.NameTooLong.Code, result.Error.Code);
    }

    [Fact]
    public void Create_WithBlankDeliveryTime_Fails()
    {
        var result = DeliveryMethod.Create(DeliveryMethodId.New(), "Express", "  ", Price());

        Assert.True(result.IsFailure);
        Assert.Equal(DeliveryMethodErrors.DeliveryTimeRequired.Code, result.Error.Code);
    }

    [Fact]
    public void Create_WithTooLongDeliveryTime_Fails()
    {
        var result = DeliveryMethod.Create(DeliveryMethodId.New(), "Express", new string('d', DeliveryMethod.MaxDeliveryTimeLength + 1), Price());

        Assert.True(result.IsFailure);
        Assert.Equal(DeliveryMethodErrors.DeliveryTimeTooLong.Code, result.Error.Code);
    }

    [Fact]
    public void Create_WithNullPrice_Fails()
    {
        var result = DeliveryMethod.Create(DeliveryMethodId.New(), "Express", "1-2 days", null!);

        Assert.True(result.IsFailure);
        Assert.Equal(DeliveryMethodErrors.PriceRequired.Code, result.Error.Code);
    }

    [Fact]
    public void Create_WithNegativePrice_Fails()
    {
        var result = DeliveryMethod.Create(DeliveryMethodId.New(), "Express", "1-2 days", NegativePrice());

        Assert.True(result.IsFailure);
        Assert.Equal(DeliveryMethodErrors.InvalidPrice.Code, result.Error.Code);
    }

    [Fact]
    public void Create_WithFreeDelivery_Succeeds()
    {
        var result = DeliveryMethod.Create(DeliveryMethodId.New(), "Free Shipping", "5-7 days", Price(0m));

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value.Price.Amount);
    }

    [Fact]
    public void Create_TrimsNameAndDeliveryTime()
    {
        var result = DeliveryMethod.Create(DeliveryMethodId.New(), "  Express  ", "  1-2 days  ", Price());

        Assert.True(result.IsSuccess);
        Assert.Equal("Express", result.Value.Name);
        Assert.Equal("1-2 days", result.Value.DeliveryTime);
    }

    [Fact]
    public void Create_RaisesDeliveryMethodCreatedDomainEvent()
    {
        var deliveryMethod = Create();

        var domainEvent = Assert.Single(deliveryMethod.GetDomainEvents());
        Assert.IsType<DeliveryMethodCreatedDomainEvent>(domainEvent);
    }

    [Fact]
    public void Rename_WithValidName_UpdatesName()
    {
        var deliveryMethod = Create();

        var result = deliveryMethod.Rename("  Same Day  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Same Day", deliveryMethod.Name);
    }

    [Fact]
    public void Rename_WithBlankName_Fails()
    {
        var deliveryMethod = Create();

        var result = deliveryMethod.Rename("   ");

        Assert.True(result.IsFailure);
        Assert.Equal(DeliveryMethodErrors.NameRequired.Code, result.Error.Code);
        Assert.Equal("Standard Delivery", deliveryMethod.Name);
    }

    [Fact]
    public void Rename_WithTooLongName_Fails()
    {
        var deliveryMethod = Create();

        var result = deliveryMethod.Rename(new string('a', DeliveryMethod.MaxNameLength + 1));

        Assert.True(result.IsFailure);
        Assert.Equal(DeliveryMethodErrors.NameTooLong.Code, result.Error.Code);
    }

    [Fact]
    public void Rename_RaisesDeliveryMethodRenamedDomainEvent()
    {
        var deliveryMethod = Create();
        deliveryMethod.ClearDomainEvents();

        var result = deliveryMethod.Rename("Express");

        Assert.True(result.IsSuccess);
        var domainEvent = Assert.Single(deliveryMethod.GetDomainEvents());
        Assert.IsType<DeliveryMethodRenamedDomainEvent>(domainEvent);
    }

    [Fact]
    public void ChangeDeliveryTime_WithValidValue_UpdatesDeliveryTime()
    {
        var deliveryMethod = Create();

        var result = deliveryMethod.ChangeDeliveryTime("  Within 6 hours  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Within 6 hours", deliveryMethod.DeliveryTime);
    }

    [Fact]
    public void ChangeDeliveryTime_WithBlankValue_Fails()
    {
        var deliveryMethod = Create();

        var result = deliveryMethod.ChangeDeliveryTime("  ");

        Assert.True(result.IsFailure);
        Assert.Equal(DeliveryMethodErrors.DeliveryTimeRequired.Code, result.Error.Code);
        Assert.Equal("3-5 business days", deliveryMethod.DeliveryTime);
    }

    [Fact]
    public void ChangeDeliveryTime_RaisesDeliveryMethodDeliveryTimeChangedDomainEvent()
    {
        var deliveryMethod = Create();
        deliveryMethod.ClearDomainEvents();

        var result = deliveryMethod.ChangeDeliveryTime("1-2 days");

        Assert.True(result.IsSuccess);
        var domainEvent = Assert.Single(deliveryMethod.GetDomainEvents());
        Assert.IsType<DeliveryMethodDeliveryTimeChangedDomainEvent>(domainEvent);
    }

    [Fact]
    public void ChangePrice_WithValidPrice_UpdatesPrice()
    {
        var deliveryMethod = Create();

        var result = deliveryMethod.ChangePrice(Price(75m, Currency.Egp));

        Assert.True(result.IsSuccess);
        Assert.Equal(75m, deliveryMethod.Price.Amount);
        Assert.Equal(Currency.Egp, deliveryMethod.Price.Currency);
    }

    [Fact]
    public void ChangePrice_WithNegativePrice_Fails()
    {
        var deliveryMethod = Create();

        var result = deliveryMethod.ChangePrice(NegativePrice());

        Assert.True(result.IsFailure);
        Assert.Equal(DeliveryMethodErrors.InvalidPrice.Code, result.Error.Code);
        Assert.Equal(30m, deliveryMethod.Price.Amount);
    }

    [Fact]
    public void ChangePrice_RaisesDeliveryMethodPriceChangedDomainEvent()
    {
        var deliveryMethod = Create();
        deliveryMethod.ClearDomainEvents();

        var result = deliveryMethod.ChangePrice(Price(45m));

        Assert.True(result.IsSuccess);
        var domainEvent = Assert.Single(deliveryMethod.GetDomainEvents());
        Assert.IsType<DeliveryMethodPriceChangedDomainEvent>(domainEvent);
    }
}
