using ECommerce.Application.Common;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.DeliveryMethods;
using ECommerce.Application.Orders;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Baskets;
using ECommerce.Domain.Entities.DeliveryMethods;
using ECommerce.Domain.Entities.Orders;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.Entities.UserAddresses;
using ECommerce.Domain.Repositories;
using ECommerce.Domain.ValueObjects;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Orders.Commands;

public sealed record CheckoutCommand(Guid DeliveryMethodId, Guid ShippingAddressId) : IRequest<Result<CheckoutResponse>>;

public sealed class CheckoutCommandHandler(
    IOrderRepository orders,
    IDeliveryMethodRepository deliveryMethods,
    IProductRepository products,
    IApplicationDbContext db,
    IBasketStore basketStore,
    ICurrentUserService currentUser,
    IPaymentService paymentService,
    IUnitOfWork unitOfWork) : IRequestHandler<CheckoutCommand, Result<CheckoutResponse>>
{
    public async Task<Result<CheckoutResponse>> Handle(CheckoutCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result.Failure<CheckoutResponse>(OrderErrors.InvalidUserId);

        // 1. Shipping address must exist and belong to the current user.
        var address = await db.UserAddresses
            .Where(a => a.Id == new UserAddressId(request.ShippingAddressId) && a.UserId == userId.Value)
            .Select(a => new
            {
                a.Address.RecipientFirstName,
                a.Address.RecipientLastName,
                a.Address.PhoneNumber,
                a.Address.Country,
                a.Address.City,
                a.Address.Street,
                a.Address.PostalCode,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (address is null)
            return Result.Failure<CheckoutResponse>(UserAddressErrors.NotFound);

        var addressResult = Address.Create(
            address.RecipientFirstName,
            address.RecipientLastName,
            address.PhoneNumber,
            address.Country,
            address.City,
            address.Street,
            address.PostalCode);

        if (addressResult.IsFailure)
            return Result.Failure<CheckoutResponse>(addressResult.Error);

        // 2. Delivery method must exist and be available (order snapshots its values).
        var deliveryMethod = await deliveryMethods.GetByIdAsync(new DeliveryMethodId(request.DeliveryMethodId), cancellationToken);
        if (deliveryMethod is null)
            return Result.Failure<CheckoutResponse>(DeliveryMethodErrors.NotFound);

        if (!deliveryMethod.IsAvailable)
            return Result.Failure<CheckoutResponse>(DeliveryMethodErrors.NotAvailable);

        // 3. Basket must be non-empty.
        var basket = await basketStore.GetAsync(userId.Value, cancellationToken);
        if (basket is null || basket.Items.Count == 0)
            return Result.Failure<CheckoutResponse>(OrderErrors.NoOrderItems);

        // 4. Re-validate cached basket prices against live products.
        foreach (var item in basket.Items)
        {
            var product = await products.GetByIdAsync(item.ProductId, cancellationToken);

            if (product is null)
                return Result.Failure<CheckoutResponse>(Error.NotFound(
                    "Order.ProductNotFound",
                    $"Product '{item.ProductName}' is no longer available."));

            if (product.Price.Amount != item.Price.Amount
                || product.Price.Currency.Code != item.Price.Currency.Code)
                return Result.Failure<CheckoutResponse>(Error.Validation(
                    "Order.ProductPriceChanged",
                    $"Price of '{item.ProductName}' changed from {item.Price.Amount} {item.Price.Currency.Code} to {product.Price.Amount} {product.Price.Currency.Code}. Please review your basket."));
        }

        // 5. Cancel previous pending orders so a new checkout cannot stack live payment intents.
        var newOrderId = OrderId.New();

        var pendingOrders = await orders.GetPendingByUserAsync(userId.Value, cancellationToken);
        foreach (var pendingOrder in pendingOrders)
        {
            var cancelResult = pendingOrder.CancelOrder($"Superseded by order {newOrderId}.");
            if (cancelResult.IsFailure)
                continue; // paid/advanced between reads — leave it alone

            if (!string.IsNullOrWhiteSpace(pendingOrder.PaymentIntentId))
                await paymentService.CancelPaymentIntentAsync(pendingOrder.PaymentIntentId, cancellationToken);
        }

        // 6. Create the order (domain validates item/fee currency consistency).
        var basketItems = basket.Items
            .Select(i => new BasketItemDto(i.ProductId, i.ProductName, i.PictureUrl, i.Price, i.Quantity))
            .ToList();

        var orderResult = Order.Create(
            newOrderId,
            userId.Value,
            addressResult.Value,
            basketItems,
            OrderDeliveryMethod.FromEntity(deliveryMethod));

        if (orderResult.IsFailure)
            return Result.Failure<CheckoutResponse>(orderResult.Error);

        var order = orderResult.Value;
        orders.Add(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 7. Create the payment intent (after the order is persisted; idempotency key is the order id).
        var intentResult = await paymentService.CreatePaymentIntentAsync(order, cancellationToken);
        if (intentResult.IsFailure)
        {
            // Kill the dangling order so the user can retry checkout cleanly.
            order.CancelOrder("Payment initialization failed");
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Failure<CheckoutResponse>(intentResult.Error);
        }

        var intentResultAssign = order.SetPaymentIntentId(intentResult.Value.PaymentIntentId);
        if (intentResultAssign.IsFailure)
            return Result.Failure<CheckoutResponse>(intentResultAssign.Error);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 8. The basket is consumed by checkout; empty it.
        basket.Clear();
        await basketStore.SaveAsync(basket, cancellationToken);

        return new CheckoutResponse(
            order.Id.Value,
            order.Status.ToString(),
            order.Total.Amount,
            order.Total.Currency.Code,
            intentResult.Value.PaymentIntentId,
            intentResult.Value.ClientSecret);
    }
}

public sealed class CheckoutCommandValidator : AbstractValidator<CheckoutCommand>
{
    public CheckoutCommandValidator()
    {
        RuleFor(c => c.DeliveryMethodId)
            .NotEqual(Guid.Empty);

        RuleFor(c => c.ShippingAddressId)
            .NotEqual(Guid.Empty);
    }
}
