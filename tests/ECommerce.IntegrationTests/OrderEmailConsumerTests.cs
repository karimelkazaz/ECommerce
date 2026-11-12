using ECommerce.Application.Auth;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Orders;
using ECommerce.Application.Orders.EventHandlers;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.DeliveryMethods;
using ECommerce.Domain.Entities.Inventory;
using ECommerce.Domain.Entities.Orders;
using ECommerce.Domain.Entities.Orders.Events;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.Entities.ProductsBrand;
using ECommerce.Domain.Entities.ProductsType;
using ECommerce.Domain.Entities.UserAddresses;
using ECommerce.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ECommerce.IntegrationTests;

public class OrderEmailConsumerTests
{
    private readonly FakeInbox _inbox = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeOutboxMessageContext _messageContext = new();
    private readonly FakeOutboxWriter _outboxWriter = new();
    private readonly FakeEmailSender _emailSender = new();
    private readonly FakeInventoryItemRepository _inventory = new();
    private readonly FakeOrderDbContext _db = new();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public async Task OrderCreatedListener_SnapshotsOrderAndRecipient_IntoConfirmationEmailFollowUp()
    {
        var order = SeedOrder();
        var identity = new FakeIdentityService(new UserProfileResponse(
            _userId, "Ada", "Lovelace", "ada@example.com", ["Customer"]));

        var handler = new OrderCreatedDomainEventHandler(
            _inbox, _unitOfWork, _messageContext, _db, identity, _outboxWriter,
            NullLogger<OrderCreatedDomainEventHandler>.Instance);

        await handler.Handle(new OrderCreatedDomainEvent(order.Id), CancellationToken.None);

        var followUp = Assert.Single(_outboxWriter.Messages);
        var notification = Assert.IsType<OrderConfirmationEmailNotification>(followUp);
        Assert.Equal(order.Id, notification.OrderId);
        Assert.Equal("ada@example.com", notification.CustomerEmail);
        Assert.Equal("Ada", notification.CustomerFirstName);
        Assert.Equal(20m, notification.SubTotalAmount);
        Assert.Equal(5m, notification.ShippingFeeAmount);
        Assert.Equal(25m, notification.TotalAmount);
        Assert.Equal("USD", notification.CurrencyCode);

        var line = Assert.Single(notification.Lines);
        Assert.Equal("Classic Tee", line.ProductName);
        Assert.Equal(1, line.Quantity);
        Assert.Equal(20m, line.PriceAmount);
    }

    [Fact]
    public async Task OrderCreatedListener_WhenOrderMissing_SendsNothing()
    {
        var identity = new FakeIdentityService(new UserProfileResponse(
            _userId, "Ada", "Lovelace", "ada@example.com", ["Customer"]));

        var handler = new OrderCreatedDomainEventHandler(
            _inbox, _unitOfWork, _messageContext, _db, identity, _outboxWriter,
            NullLogger<OrderCreatedDomainEventHandler>.Instance);

        await handler.Handle(new OrderCreatedDomainEvent(OrderId.New()), CancellationToken.None);

        Assert.Empty(_outboxWriter.Messages);
    }

    [Fact]
    public async Task OrderCreatedListener_WhenProfileMissing_SendsNothing()
    {
        var order = SeedOrder();

        var handler = new OrderCreatedDomainEventHandler(
            _inbox, _unitOfWork, _messageContext, _db, new FakeIdentityService(), _outboxWriter,
            NullLogger<OrderCreatedDomainEventHandler>.Instance);

        await handler.Handle(new OrderCreatedDomainEvent(order.Id), CancellationToken.None);

        Assert.Empty(_outboxWriter.Messages);
    }

    [Fact]
    public async Task OrderPaymentFailedListener_ReleasesStockAndEnqueuesRecoveryEmailFollowUp()
    {
        var order = SeedOrder();
        // Outstanding reserve entry (order's product) the consumer must replay.
        var productId = order.OrderItems.Single().ProductId;
        var inventoryItem = InventoryItem.Create(productId, 10).Value;
        Assert.True(inventoryItem.Reserve(order.Id, 2).IsSuccess); // available 10 -> 8
        _inventory.Add(inventoryItem);

        var identity = new FakeIdentityService(new UserProfileResponse(
            _userId, "Ada", "Lovelace", "ada@example.com", ["Customer"]));

        var handler = new OrderPaymentFailedDomainEventHandler(
            _db, _inventory, identity, _outboxWriter, _inbox, _unitOfWork, _messageContext,
            NullLogger<OrderPaymentFailedDomainEventHandler>.Instance);

        await handler.Handle(new OrderPaymentFailedDomainEvent(order.Id), CancellationToken.None);

        Assert.Equal(10, inventoryItem.AvailableQuantity); // released back to 10

        var followUp = Assert.Single(_outboxWriter.Messages);
        var notification = Assert.IsType<OrderPaymentFailedEmailNotification>(followUp);
        Assert.Equal(order.Id, notification.OrderId);
        Assert.Equal("ada@example.com", notification.CustomerEmail);
        Assert.Equal("Ada", notification.CustomerFirstName);
        Assert.Equal(25m, notification.TotalAmount);
        Assert.Equal("USD", notification.CurrencyCode);
    }

    [Fact]
    public async Task ConfirmationEmailSender_FormatsAndDeliversBody()
    {
        var orderId = OrderId.New();
        var notification = new OrderConfirmationEmailNotification(
            orderId,
            "ada@example.com",
            "Ada",
            [new EmailLine("Named <product>", 2, 12.5m)],
            25m,
            5m,
            30m,
            "USD");

        var handler = new OrderConfirmationEmailRequestHandler(
            _inbox, _unitOfWork, _messageContext, _emailSender,
            NullLogger<OrderConfirmationEmailRequestHandler>.Instance);

        await handler.Handle(notification, CancellationToken.None);

        var sent = Assert.Single(_emailSender.Sent);
        Assert.Equal("ada@example.com", sent.To);
        Assert.Equal($"Order confirmation — {orderId.Value:N}", sent.Subject);
        Assert.Contains("Hi Ada", sent.HtmlBody);
        Assert.Contains("&lt;product&gt;", sent.HtmlBody);
        Assert.Contains("USD 25.00", sent.HtmlBody);
    }

    [Fact]
    public async Task PaymentFailedEmailSender_FormatsAndDeliversBody()
    {
        var orderId = OrderId.New();
        var notification = new OrderPaymentFailedEmailNotification(
            orderId, "ada@example.com", "Ada", 30m, "USD");

        var handler = new OrderPaymentFailedEmailRequestHandler(
            _inbox, _unitOfWork, _messageContext, _emailSender,
            NullLogger<OrderPaymentFailedEmailRequestHandler>.Instance);

        await handler.Handle(notification, CancellationToken.None);

        var sent = Assert.Single(_emailSender.Sent);
        Assert.Equal("ada@example.com", sent.To);
        Assert.Equal($"Payment failed — order {orderId.Value:N}", sent.Subject);
        Assert.Contains("Hi Ada", sent.HtmlBody);
        Assert.Contains("USD 30.00", sent.HtmlBody);
    }

    [Fact]
    public async Task OrderCancelledListener_ReleasesStockAndEnqueuesCancellationEmailFollowUp()
    {
        var order = SeedOrder();
        Assert.True(order.CancelOrder("Out of stock").IsSuccess);

        // Outstanding reserve entry (order's product) the consumer must replay.
        var productId = order.OrderItems.Single().ProductId;
        var inventoryItem = InventoryItem.Create(productId, 10).Value;
        Assert.True(inventoryItem.Reserve(order.Id, 2).IsSuccess); // available 10 -> 8
        _inventory.Add(inventoryItem);

        var identity = new FakeIdentityService(new UserProfileResponse(
            _userId, "Ada", "Lovelace", "ada@example.com", ["Customer"]));

        var handler = new OrderCancelledDomainEventHandler(
            _db, _inventory, identity, _outboxWriter, _inbox, _unitOfWork, _messageContext,
            NullLogger<OrderCancelledDomainEventHandler>.Instance);

        await handler.Handle(new OrderCancelledDomainEvent(order.Id), CancellationToken.None);

        Assert.Equal(10, inventoryItem.AvailableQuantity); // released back to 10

        var followUp = Assert.Single(_outboxWriter.Messages);
        var notification = Assert.IsType<OrderCancelledEmailNotification>(followUp);
        Assert.Equal(order.Id, notification.OrderId);
        Assert.Equal("ada@example.com", notification.CustomerEmail);
        Assert.Equal("Ada", notification.CustomerFirstName);
        Assert.Equal(25m, notification.TotalAmount);
        Assert.Equal("USD", notification.CurrencyCode);
        Assert.Equal("Out of stock", notification.CancellationReason);
    }

    [Fact]
    public async Task OrderCancelledListener_WhenProfileMissing_SendsNothing()
    {
        var order = SeedOrder();
        Assert.True(order.CancelOrder("test").IsSuccess);

        var handler = new OrderCancelledDomainEventHandler(
            _db, _inventory, new FakeIdentityService(), _outboxWriter, _inbox, _unitOfWork, _messageContext,
            NullLogger<OrderCancelledDomainEventHandler>.Instance);

        await handler.Handle(new OrderCancelledDomainEvent(order.Id), CancellationToken.None);

        Assert.Empty(_outboxWriter.Messages);
    }

    [Fact]
    public async Task CancelledEmailSender_FormatsAndDeliversBody()
    {
        var orderId = OrderId.New();
        var notification = new OrderCancelledEmailNotification(
            orderId, "ada@example.com", "Ada", 30m, "USD", "Cancelled by admin");

        var handler = new OrderCancelledEmailRequestHandler(
            _inbox, _unitOfWork, _messageContext, _emailSender,
            NullLogger<OrderCancelledEmailRequestHandler>.Instance);

        await handler.Handle(notification, CancellationToken.None);

        var sent = Assert.Single(_emailSender.Sent);
        Assert.Equal("ada@example.com", sent.To);
        Assert.Equal($"Order cancelled — {orderId.Value:N}", sent.Subject);
        Assert.Contains("Hi Ada", sent.HtmlBody);
        Assert.Contains("has been cancelled", sent.HtmlBody);
        Assert.Contains("Cancelled by admin", sent.HtmlBody);
        Assert.Contains("USD 30.00", sent.HtmlBody);
    }

    [Fact]
    public async Task CancelledEmailSender_WithoutReason_OmitsReasonLine()
    {
        var notification = new OrderCancelledEmailNotification(
            OrderId.New(), "ada@example.com", "Ada", 30m, "USD", null);

        var handler = new OrderCancelledEmailRequestHandler(
            _inbox, _unitOfWork, _messageContext, _emailSender,
            NullLogger<OrderCancelledEmailRequestHandler>.Instance);

        await handler.Handle(notification, CancellationToken.None);

        var sent = Assert.Single(_emailSender.Sent);
        Assert.DoesNotContain("Reason:", sent.HtmlBody);
    }

    private Order SeedOrder(string? productName = null)
    {
        var product = Domain.Entities.Products.Product.Create(
            ProductId.New(), productName ?? "Classic Tee", "A comfortable t-shirt.",
            "https://img.example.com/tee.png", Money.Create(20m, Currency.Usd).Value).Value;

        var delivery = TestDelivery();

        var order = Domain.Entities.Orders.Order.Create(
            OrderId.New(),
            _userId,
            Address.Create("John", "Doe", "+20 100 000 0000", "Egypt", "Cairo", "1 Main Street", "11511").Value,
            [new BasketItemDto(product.Id, product.Name, product.PictureUrl, product.Price, 1)],
            OrderDeliveryMethod.FromEntity(delivery)).Value;

        Assert.True(order.SetPaymentIntentId("pi_test_intent").IsSuccess);
        _db.OrdersList.Add(order);
        return order;
    }

    private static DeliveryMethod TestDelivery(decimal fee = 5m)
        => DeliveryMethod.Create(
            DeliveryMethodId.New(),
            "Standard",
            "3-5 business days",
            Money.Create(fee, Currency.Usd).Value).Value;
}
