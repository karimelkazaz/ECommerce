using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.DeliveryMethods;
using ECommerce.Domain.Entities.Orders;
using ECommerce.Domain.Entities.Orders.Events;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.ValueObjects;
using Xunit;

namespace ECommerce.Domain.UnitTests;

public class OrderTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static Address CreateAddress()
        => Address.Create(
            "John",
            "Doe",
            "+20 100 000 0000",
            "Egypt",
            "Cairo",
            "1 Main Street",
            "11511").Value;

    private static OrderDeliveryMethod CreateDeliveryMethod(decimal fee = 30m, Currency? currency = null)
    {
        var deliveryMethod = DeliveryMethod.Create(
            DeliveryMethodId.New(),
            "Standard",
            "3-5 business days",
            Money.Create(fee, currency ?? Currency.Usd).Value).Value;

        return OrderDeliveryMethod.FromEntity(deliveryMethod);
    }

    private static BasketItemDto BasketItem(decimal amount = 100m, int quantity = 1, Currency? currency = null)
        => new(ProductId.New(), "T-Shirt", "tshirt.png", Money.Create(amount, currency ?? Currency.Usd).Value, quantity);

    private static Result<Order> CreateOrder(params BasketItemDto[] items)
        => Order.Create(OrderId.New(), UserId, CreateAddress(), items, CreateDeliveryMethod());

    private static Order PaidOrder(string paymentIntentId = "pi_test_123")
    {
        var order = CreateOrder(BasketItem()).Value;
        var result = order.MarkAsPaid(paymentIntentId);
        Assert.True(result.IsSuccess);
        return order;
    }

    [Fact]
    public void Create_WithValidItems_SucceedsAndComputesTotals()
    {
        var result = CreateOrder(BasketItem(amount: 100m, quantity: 2), BasketItem(amount: 50m));

        Assert.True(result.IsSuccess);
        var order = result.Value;
        Assert.Equal(250m, order.SubTotal.Amount);
        Assert.Equal(30m, order.ShippingFee.Amount);
        Assert.Equal(280m, order.Total.Amount);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(2, order.OrderItems.Count);
    }

    [Fact]
    public void Create_WithEmptyBasket_Fails()
    {
        var result = CreateOrder();

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.NoOrderItems.Code, result.Error.Code);
    }

    [Fact]
    public void Create_WithNullAddress_Fails()
    {
        var result = Order.Create(OrderId.New(), UserId, null!, [BasketItem()], CreateDeliveryMethod());

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.InvalidAddress.Code, result.Error.Code);
    }

    [Fact]
    public void Create_WithMismatchedItemCurrency_Fails()
    {
        var result = CreateOrder(BasketItem(currency: Currency.Usd), BasketItem(currency: Currency.Eur));

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.CurrencyMismatch.Code, result.Error.Code);
    }

    [Fact]
    public void Create_WithDeliveryFeeInDifferentCurrency_Fails()
    {
        var address = CreateAddress();
        var delivery = CreateDeliveryMethod(currency: Currency.Egp);

        var result = Order.Create(OrderId.New(), UserId, address, [BasketItem()], delivery);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.CurrencyMismatch.Code, result.Error.Code);
    }

    [Fact]
    public void Create_RaisesOrderCreatedDomainEvent()
    {
        var order = CreateOrder(BasketItem()).Value;

        var domainEvent = Assert.Single(order.GetDomainEvents());
        Assert.IsType<OrderCreatedDomainEvent>(domainEvent);
    }

    [Fact]
    public void SetPaymentIntentId_WhenPending_Succeeds()
    {
        var order = CreateOrder(BasketItem()).Value;

        var result = order.SetPaymentIntentId("pi_test_123");

        Assert.True(result.IsSuccess);
        Assert.Equal("pi_test_123", order.PaymentIntentId);
    }

    [Fact]
    public void SetPaymentIntentId_AfterPayment_Fails()
    {
        var order = PaidOrder();

        var result = order.SetPaymentIntentId("pi_other");

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void MarkAsPaid_FromPending_TransitionsToProcessing()
    {
        var order = CreateOrder(BasketItem()).Value;

        var before = DateTimeOffset.UtcNow;
        var result = order.MarkAsPaid("pi_test_123");
        var after = DateTimeOffset.UtcNow;

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Processing, order.Status);
        Assert.Equal("pi_test_123", order.PaymentIntentId);
        Assert.NotNull(order.PaidAtUtc);
        Assert.InRange(order.PaidAtUtc!.Value, before, after);
    }

    [Fact]
    public void MarkAsPaid_IsIdempotentForSamePaymentIntent()
    {
        var order = PaidOrder("pi_test_123");
        var paidAt = order.PaidAtUtc;

        var result = order.MarkAsPaid("pi_test_123");

        Assert.True(result.IsSuccess);
        Assert.Equal(paidAt, order.PaidAtUtc);
    }

    [Fact]
    public void MarkAsPaid_WithDifferentIntentAfterPayment_Fails()
    {
        var order = PaidOrder("pi_test_123");

        var result = order.MarkAsPaid("pi_other");

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.InvalidPaymentState.Code, result.Error.Code);
    }

    [Fact]
    public void MarkAsPaid_WhenCancelled_Fails()
    {
        var order = CreateOrder(BasketItem()).Value;
        Assert.True(order.CancelOrder().IsSuccess);

        var result = order.MarkAsPaid("pi_test_123");

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.CannotPayCancelled.Code, result.Error.Code);
    }

    [Fact]
    public void MarkAsPaymentFailed_FromPending_CancelsOrder()
    {
        var order = CreateOrder(BasketItem()).Value;

        var result = order.MarkAsPaymentFailed();

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void MarkAsPaymentFailed_AfterPayment_Fails()
    {
        var order = PaidOrder();

        var result = order.MarkAsPaymentFailed();

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void ShipOrder_FromProcessing_Succeeds()
    {
        var order = PaidOrder();

        var result = order.ShipOrder();

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Shipped, order.Status);
    }

    [Fact]
    public void ShipOrder_FromPending_Fails()
    {
        var order = CreateOrder(BasketItem()).Value;

        var result = order.ShipOrder();

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.InvalidStateTransition.Code, result.Error.Code);
    }

    [Fact]
    public void ShipOrder_Twice_Fails()
    {
        var order = PaidOrder();
        Assert.True(order.ShipOrder().IsSuccess);

        var result = order.ShipOrder();

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void DeliverOrder_FromShipped_Succeeds()
    {
        var order = PaidOrder();
        Assert.True(order.ShipOrder().IsSuccess);

        var result = order.DeliverOrder();

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Delivered, order.Status);
    }

    [Fact]
    public void DeliverOrder_FromPending_Fails()
    {
        var order = CreateOrder(BasketItem()).Value;

        var result = order.DeliverOrder();

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void CancelOrder_FromPending_Succeeds()
    {
        var order = CreateOrder(BasketItem()).Value;

        var result = order.CancelOrder();

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void CancelOrder_FromProcessing_Succeeds()
    {
        var order = PaidOrder();

        var result = order.CancelOrder();

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void CancelOrder_WhenShipped_Fails()
    {
        var order = PaidOrder();
        Assert.True(order.ShipOrder().IsSuccess);

        var result = order.CancelOrder();

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void FullLifecycle_PendingToDelivered_Succeeds()
    {
        var order = CreateOrder(BasketItem()).Value;

        Assert.True(order.SetPaymentIntentId("pi_test_123").IsSuccess);
        Assert.True(order.MarkAsPaid("pi_test_123").IsSuccess);
        Assert.Equal(OrderStatus.Processing, order.Status);

        Assert.True(order.ShipOrder().IsSuccess);
        Assert.Equal(OrderStatus.Shipped, order.Status);

        Assert.True(order.DeliverOrder().IsSuccess);
        Assert.Equal(OrderStatus.Delivered, order.Status);
    }

    [Fact]
    public void Create_WithNullItemPrice_Fails()
    {
        var result = Order.Create(
            OrderId.New(),
            UserId,
            CreateAddress(),
            [new BasketItemDto(ProductId.New(), "T-Shirt", "tshirt.png", null!, 1)],
            CreateDeliveryMethod());

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.InvalidItemPrice.Code, result.Error.Code);
    }

    [Fact]
    public void Create_WithFreeDelivery_Succeeds()
    {
        var result = Order.Create(
            OrderId.New(),
            UserId,
            CreateAddress(),
            [BasketItem(amount: 100m)],
            CreateDeliveryMethod(fee: 0m));

        Assert.True(result.IsSuccess);
        Assert.Equal(100m, result.Value.SubTotal.Amount);
        Assert.Equal(0m, result.Value.ShippingFee.Amount);
        Assert.Equal(100m, result.Value.Total.Amount);
    }

    [Fact]
    public void CancelOrder_StoresReasonAndTimestamp()
    {
        var order = CreateOrder(BasketItem()).Value;
        var before = DateTimeOffset.UtcNow;

        var result = order.CancelOrder("  Superseded by order 42  ");
        var after = DateTimeOffset.UtcNow;

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal("Superseded by order 42", order.CancellationReason);
        Assert.InRange(order.CancelledAtUtc!.Value, before, after);
    }

    [Fact]
    public void CancelOrder_WithoutReason_StoresOnlyTimestamp()
    {
        var order = CreateOrder(BasketItem()).Value;
        var before = DateTimeOffset.UtcNow;

        Assert.True(order.CancelOrder().IsSuccess);
        var after = DateTimeOffset.UtcNow;

        Assert.Null(order.CancellationReason);
        Assert.InRange(order.CancelledAtUtc!.Value, before, after);
    }

    [Fact]
    public void CancelOrder_RaisesOrderCancelledDomainEvent()
    {
        var order = CreateOrder(BasketItem()).Value;
        order.ClearDomainEvents();

        Assert.True(order.CancelOrder("customer request").IsSuccess);

        var domainEvent = Assert.Single(order.GetDomainEvents());
        Assert.IsType<OrderCancelledDomainEvent>(domainEvent);
    }

    [Fact]
    public void ShipOrder_RecordsShippedAtUtc()
    {
        var order = PaidOrder();
        var before = DateTimeOffset.UtcNow;

        Assert.True(order.ShipOrder().IsSuccess);
        var after = DateTimeOffset.UtcNow;

        Assert.Equal(OrderStatus.Shipped, order.Status);
        Assert.InRange(order.ShippedAtUtc!.Value, before, after);
    }

    [Fact]
    public void DeliverOrder_RecordsDeliveredAtUtc()
    {
        var order = PaidOrder();
        Assert.True(order.ShipOrder().IsSuccess);
        var before = DateTimeOffset.UtcNow;

        Assert.True(order.DeliverOrder().IsSuccess);
        var after = DateTimeOffset.UtcNow;

        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.InRange(order.DeliveredAtUtc!.Value, before, after);
    }

    [Fact]
    public void Refund_FromPending_Fails()
    {
        var order = CreateOrder(BasketItem()).Value;

        var result = order.Refund();

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.NotRefundable.Code, result.Error.Code);
    }

    [Fact]
    public void Refund_AfterCancelled_Fails()
    {
        var order = CreateOrder(BasketItem()).Value;
        Assert.True(order.CancelOrder("customer").IsSuccess);

        Assert.True(order.Refund().IsFailure);
    }

    [Fact]
    public void Refund_FromProcessing_Succeeds()
    {
        var order = PaidOrder();
        var before = DateTimeOffset.UtcNow;

        var result = order.Refund();
        var after = DateTimeOffset.UtcNow;

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Refunded, order.Status);
        Assert.InRange(order.RefundedAtUtc!.Value, before, after);
    }

    [Fact]
    public void Refund_AfterDelivered_Succeeds()
    {
        var order = PaidOrder();
        Assert.True(order.ShipOrder().IsSuccess);
        Assert.True(order.DeliverOrder().IsSuccess);

        Assert.True(order.Refund().IsSuccess);
        Assert.Equal(OrderStatus.Refunded, order.Status);
    }

    [Fact]
    public void Refund_Twice_IsIdempotentWithoutSecondEvent()
    {
        var order = PaidOrder();
        order.ClearDomainEvents();
        Assert.True(order.Refund().IsSuccess);

        var result = order.Refund();

        Assert.True(result.IsSuccess);
        var events = order.GetDomainEvents();
        Assert.Single(events);
        Assert.IsType<OrderRefundedDomainEvent>(events[0]);
    }
}
