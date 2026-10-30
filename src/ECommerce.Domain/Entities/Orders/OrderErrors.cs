using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities.Orders;

public static class OrderErrors
{
    public static readonly Error NotFound = Error.NotFound("Order.NotFound", "Order not found.");
    public static readonly Error InvalidId = Error.Validation("Order.InvalidId", "Invalid order ID.");
    public static readonly Error InvalidUserId = Error.Validation("Order.InvalidUserId", "User ID is required.");
    public static readonly Error NoOrderItems = Error.Validation("Order.NoOrderItems", "An order must contain at least one item.");
    public static readonly Error InvalidAddress = Error.Validation("Order.InvalidAddress", "Shipping address is required.");
    public static readonly Error InvalidDeliveryMethod = Error.Validation("Order.InvalidDeliveryMethod", "Delivery method is required.");
    public static readonly Error InvalidDeliveryFee = Error.Validation("Order.InvalidDeliveryFee", "Delivery fee is required.");
    public static readonly Error CurrencyMismatch = Error.Validation("Order.CurrencyMismatch", "Currencies must match across all items and fees.");
    public static readonly Error InvalidStateTransition = Error.Validation("Order.InvalidStateTransition", "Order is not in a valid state for this operation.");
    public static readonly Error InvalidPaymentIntent = Error.Validation("Order.InvalidPaymentIntent", "Payment intent is required.");
    public static readonly Error CannotPayCancelled = Error.Validation("Order.CannotPayCancelled", "Cannot pay a cancelled order.");
    public static readonly Error InvalidPaymentState = Error.Validation("Order.InvalidPaymentState", "Order is not in a valid state for payment processing.");
    public static readonly Error PaymentIntentMismatch = Error.Validation("Order.PaymentIntentMismatch", "Payment intent does not match the expected value.");
    public static readonly Error InvalidItemId =
    Error.Validation("Order.InvalidItemId", "Order item id is required.");

    public static readonly Error InvalidProductId =
        Error.Validation("Order.InvalidProductId", "Product id is required.");
    public static readonly Error InvalidQuantity =
        Error.Validation("Order.InvalidQuantity", "Quantity must be at least 1.");

    public static readonly Error InvalidItemPrice =
        Error.Validation("Order.InvalidItemPrice", "Item price is required.");

    public static readonly Error ProductNotFound =
        Error.NotFound("Order.ProductNotFound", "One or more products in your basket are no longer available.");

    public static readonly Error ProductPriceChanged =
        Error.Validation("Order.ProductPriceChanged", "The price of one or more items in your basket has changed.");

    public static readonly Error InvalidTotal =
        Error.Validation("Order.InvalidTotal", "Order total must be greater than zero.");

    public static readonly Error NotRefundable =
        Error.Validation("Order.NotRefundable", "Order is not in a valid state for a refund.");
}
