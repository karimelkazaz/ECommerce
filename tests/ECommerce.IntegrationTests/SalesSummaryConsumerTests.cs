using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Orders.EventHandlers;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.DeliveryMethods;
using ECommerce.Domain.Entities.Orders;
using ECommerce.Domain.Entities.Orders.Events;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ECommerce.IntegrationTests;

public class SalesSummaryConsumerTests
{
    private readonly FakeInbox _inbox = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeOutboxMessageContext _messageContext = new();
    private readonly FakeOrderDbContext _db = new();
    private readonly FakeSalesSummaryRepository _sales = new();

    [Fact]
    public async Task OrderPaidConsumer_StartsNewRowOnFirstPaidOrder()
    {
        var order = SeedPaidOrder("USD", itemAmount: 20m, fee: 5m);

        await HandlePaidEventAsync(order);

        var row = Assert.Single(_sales.Rows);
        Assert.Equal("USD", row.CurrencyCode);
        Assert.Equal(25m, row.Revenue);
        Assert.Equal(1, row.OrderCount);
        Assert.Equal(1, _inbox.TotalClaimsFor("orders.order-paid"));
    }

    [Fact]
    public async Task OrderPaidConsumer_AccruesSecondOrderIntoSameRow()
    {
        var first = SeedPaidOrder();
        var second = SeedPaidOrder();

        await HandlePaidEventAsync(first);
        await HandlePaidEventAsync(second);

        var row = Assert.Single(_sales.Rows);
        Assert.Equal("USD", row.CurrencyCode);
        Assert.Equal(50m, row.Revenue);
        Assert.Equal(2, row.OrderCount);
    }

    [Fact]
    public async Task OrderPaidConsumer_KeepsCurrenciesInSeparateRows()
    {
        var usd = SeedPaidOrder("USD");
        var eur = SeedPaidOrder("EUR");

        await HandlePaidEventAsync(usd);
        await HandlePaidEventAsync(eur);

        Assert.Equal(2, _sales.Rows.Count);
        Assert.Contains(_sales.Rows, r => r.CurrencyCode == "USD" && r.Revenue == 25m && r.OrderCount == 1);
        Assert.Contains(_sales.Rows, r => r.CurrencyCode == "EUR" && r.Revenue == 25m && r.OrderCount == 1);
    }

    [Fact]
    public async Task OrderPaidConsumer_WhenOrderNotFound_SkipsAccrual()
    {
        // No orders seeded: the event refers to an unknown order.
        await HandlePaidEventAsync(OrderId.New());

        Assert.Empty(_sales.Rows);
    }

    [Fact]
    public async Task OrderPaidConsumer_WhenPaidAtMissing_SkipsAccrual()
    {
        var order = SeedPendingOrder();

        await HandlePaidEventAsync(order.Id);

        Assert.Empty(_sales.Rows);
    }

    private async Task HandlePaidEventAsync(Order order)
        => await HandlePaidEventAsync(order.Id);

    private async Task HandlePaidEventAsync(OrderId orderId)
    {
        var handler = new OrderPaidDomainEventHandler(
            _inbox, _unitOfWork, _messageContext, _db, _sales,
            NullLogger<OrderPaidDomainEventHandler>.Instance);

        await handler.Handle(new OrderPaidDomainEvent(orderId), CancellationToken.None);
    }

    private Order SeedPaidOrder(
        string currency = "USD",
        decimal itemAmount = 20m,
        decimal fee = 5m)
    {
        var order = SeedPendingOrder(currency, itemAmount, fee);
        Assert.True(order.MarkAsPaid(order.PaymentIntentId!).IsSuccess);
        return order;
    }

    private Order SeedPendingOrder(string currency = "USD", decimal itemAmount = 20m, decimal fee = 5m)
    {
        var currencyValue = Currency.FromCode(currency);
        var product = Product.Create(
            ProductId.New(), "Classic Tee", "A comfortable t-shirt.",
            "https://img.example.com/tee.png", Money.Create(itemAmount, currencyValue).Value).Value;

        var delivery = DeliveryMethod.Create(
            DeliveryMethodId.New(), "Standard", "3-5 business days",
            Money.Create(fee, currencyValue).Value).Value;

        var order = Order.Create(
            OrderId.New(),
            Guid.NewGuid(),
            Address.Create("John", "Doe", "+20 100 000 0000", "Egypt", "Cairo", "1 Main Street", "11511").Value,
            [new BasketItemDto(product.Id, product.Name, product.PictureUrl, product.Price, 1)],
            OrderDeliveryMethod.FromEntity(delivery)).Value;

        Assert.True(order.SetPaymentIntentId("pi_test_intent").IsSuccess);
        _db.OrdersList.Add(order);
        return order;
    }
}
