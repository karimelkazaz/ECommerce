using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.DeliveryMethods;
using ECommerce.Domain.Entities.Orders.Events;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities.Orders;

public class Order : Entity<OrderId>
{
    private readonly List<OrderItem> _orderItems = [];

    public Guid UserId { get; private set; }
    public DateTimeOffset? PaidAtUtc { get; private set; }
    public Address ShippingAddress { get; private set; } = null!;
    public OrderStatus Status { get; private set; }

    // Delivery Snapshot
    public OrderDeliveryMethod DeliveryMethod { get; private set; } = null!;

    // Persisted Totals
    public Money SubTotal { get; private set; } = null!;
    public Money ShippingFee { get; private set; } = null!;
    public Money Total { get; private set; } = null!;

    public string? PaymentIntentId { get; private set; }

    public IReadOnlyList<OrderItem> OrderItems => _orderItems.AsReadOnly();

    private Order() { }

    private Order(
        OrderId id,
        Guid userId,
        Address shippingAddress,
        OrderDeliveryMethod deliveryMethod,
        Money subTotal,
        Money shippingFee)
        : base(id)
    {
        UserId = userId;
        ShippingAddress = shippingAddress;
        DeliveryMethod = deliveryMethod;
        SubTotal = subTotal;
        ShippingFee = shippingFee;
        Total = subTotal.Add(shippingFee);
        Status = OrderStatus.Pending;
    }

    public static Result<Order> Create(
        OrderId id,
        Guid userId,
        Address shippingAddress,
        IReadOnlyList<BasketItemDto> basketItems,
        OrderDeliveryMethod liveDeliveryMethod)
    {
        if (id == default)
            return Result.Failure<Order>(OrderErrors.InvalidId);

        if (userId == Guid.Empty)
            return Result.Failure<Order>(OrderErrors.InvalidUserId);

        if (shippingAddress is null)
            return Result.Failure<Order>(OrderErrors.InvalidAddress);

        if (basketItems is null || basketItems.Count == 0)
            return Result.Failure<Order>(OrderErrors.NoOrderItems);

        if (liveDeliveryMethod is null)
            return Result.Failure<Order>(OrderErrors.InvalidDeliveryMethod);

        var currency = basketItems[0].Price.Currency;
        if (liveDeliveryMethod.Price.Currency != currency)
            return Result.Failure<Order>(OrderErrors.CurrencyMismatch);

        // Calculate Totals
        decimal subTotalAmount = basketItems.Sum(i => i.Price.Amount * i.Quantity);
        var subTotal = Money.Create(subTotalAmount, currency).Value;


        var order = new Order(
            id,
            userId,
            shippingAddress,
            liveDeliveryMethod,
            subTotal,
            liveDeliveryMethod.Price);

        foreach (var item in basketItems)
        {
            if (item.Price.Currency != currency)
                return Result.Failure<Order>(OrderErrors.CurrencyMismatch);

            var orderItemResult = OrderItem.Create(
                OrderItemId.New(),
                item.ProductId,
                item.ProductName,
                item.PictureUrl,
                item.Price,
                item.Quantity);

            if (orderItemResult.IsFailure)
                return Result.Failure<Order>(orderItemResult.Error);

            order._orderItems.Add(orderItemResult.Value);
        }

        order.RaiseDomainEvent(new OrderCreatedDomainEvent(order.Id));
        return Result.Success(order);
    }

    public Result SetPaymentIntentId(string paymentIntentId)
    {
        if (Status != OrderStatus.Pending)
            return Result.Failure(OrderErrors.InvalidStateTransition);
        if (string.IsNullOrWhiteSpace(paymentIntentId))
            return Result.Failure(OrderErrors.InvalidPaymentIntent);
        PaymentIntentId = paymentIntentId;
        return Result.Success();
    }

    public Result MarkAsPaid(string paymentIntentId)
    {
        if (Status == OrderStatus.Cancelled)
            return Result.Failure(OrderErrors.CannotPayCancelled);

        if (Status == OrderStatus.Processing
            && PaymentIntentId == paymentIntentId
            && PaidAtUtc is not null)
            return Result.Success();

        if (Status != OrderStatus.Pending)
            return Result.Failure(OrderErrors.InvalidPaymentState);

        if (!string.IsNullOrWhiteSpace(PaymentIntentId)
            && PaymentIntentId != paymentIntentId)
            return Result.Failure(OrderErrors.PaymentIntentMismatch);

        PaymentIntentId = paymentIntentId;
        PaidAtUtc = DateTimeOffset.UtcNow;
        Status = OrderStatus.Processing;
        RaiseDomainEvent(new OrderPaidDomainEvent(Id));

        return Result.Success();
    }


    public Result MarkAsPaymentFailed()
    {
        if (Status != OrderStatus.Pending)
            return Result.Failure(OrderErrors.InvalidStateTransition);

        Status = OrderStatus.Cancelled;
        RaiseDomainEvent(new OrderPaymentFailedDomainEvent(Id));
        return Result.Success();
    }

    public Result ShipOrder()
    {
        if (Status != OrderStatus.Processing)
            return Result.Failure(OrderErrors.InvalidStateTransition);

        Status = OrderStatus.Shipped;
        RaiseDomainEvent(new OrderShippedDomainEvent(Id));

        return Result.Success();
    }

    public Result DeliverOrder()
    {
        if (Status != OrderStatus.Shipped)
            return Result.Failure(OrderErrors.InvalidStateTransition);

        Status = OrderStatus.Delivered;
        RaiseDomainEvent(new OrderDeliveredDomainEvent(Id));

        return Result.Success();
    }

    public Result CancelOrder()
    {
        if (Status != OrderStatus.Pending && Status != OrderStatus.Processing)
            return Result.Failure(OrderErrors.InvalidStateTransition);

        Status = OrderStatus.Cancelled;
        RaiseDomainEvent(new OrderCancelledDomainEvent(Id));

        return Result.Success();
    }
}
