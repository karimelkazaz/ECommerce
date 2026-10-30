using ECommerce.Application;
using ECommerce.Application.Common;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Orders;
using ECommerce.Application.Orders.Commands;
using ECommerce.Application.Orders.Queries;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Baskets;
using ECommerce.Domain.Entities.DeliveryMethods;
using ECommerce.Domain.Entities.Orders;
using ECommerce.Domain.Entities.Orders.Events;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.Entities.ProductsBrand;
using ECommerce.Domain.Entities.ProductsType;
using ECommerce.Domain.Entities.UserAddresses;
using ECommerce.Domain.Repositories;
using ECommerce.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ECommerce.IntegrationTests;

public class OrderApplicationTests
{
    private readonly FakeOrderRepository _orderRepository = new();
    private readonly FakeDeliveryMethodRepository _deliveryMethodRepository = new();
    private readonly FakeProductRepository _productRepository = new();
    private readonly FakeBasketStore _basketStore = new();
    private readonly FakeDbContext _db = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakePaymentService _payment = new();
    private readonly FakeCurrentUserService _currentUser = new();

    public OrderApplicationTests()
    {
        _currentUser.UserId = Guid.NewGuid();
    }

    [Fact]
    public async Task CheckoutCommandHandler_WithValidBasket_CreatesOrderWithPaymentIntent()
    {
        var context = await BuildCheckoutAsync();

        var result = await context.Handler.Handle(
            new CheckoutCommand(context.DeliveryMethod.Id.Value, context.UserAddress.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Pending", result.Value.Status);
        Assert.StartsWith("pi_", result.Value.PaymentIntentId);
        Assert.Equal(25m, result.Value.Total);
        Assert.Equal("USD", result.Value.Currency);

        var order = await _orderRepository.GetByIdAsync(new OrderId(result.Value.OrderId));
        Assert.NotNull(order);
        Assert.Equal(OrderStatus.Pending, order!.Status);
        Assert.Equal(result.Value.PaymentIntentId, order.PaymentIntentId);
        Assert.Single(order.OrderItems);
        Assert.Equal(20m, order.SubTotal.Amount);
        Assert.Equal(5m, order.ShippingFee.Amount);

        var basket = await _basketStore.GetAsync(context.UserId);
        Assert.Empty(basket!.Items);
    }

    [Fact]
    public async Task CheckoutCommandHandler_SupersedesPendingOrdersAndCancelsTheirIntents()
    {
        var context = await BuildCheckoutAsync();
        var stale = await SeedPendingOrderAsync("pi_stale", context.UserId);

        var result = await context.Handler.Handle(
            new CheckoutCommand(context.DeliveryMethod.Id.Value, context.UserAddress.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(_payment.CancelledIntents);

        var staleAfter = await _orderRepository.GetByIdAsync(stale.Id);
        Assert.Equal(OrderStatus.Cancelled, staleAfter!.Status);
        Assert.Equal($"Superseded by order {result.Value.OrderId}.", staleAfter.CancellationReason);
    }

    [Fact]
    public async Task CheckoutCommandHandler_WithStalePrice_FailsAndAddsNoOrder()
    {
        // Cached basket price 20m, live product re-priced to 25m.
        var context = await BuildCheckoutAsync(productPrice: 20m);
        _productRepository.Items.Single().ChangePrice(Money.Create(25m, Currency.Usd).Value);

        var result = await context.Handler.Handle(
            new CheckoutCommand(context.DeliveryMethod.Id.Value, context.UserAddress.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.ProductPriceChanged.Code, result.Error.Code);
        Assert.Empty(_orderRepository.Items);
    }

    [Fact]
    public async Task CheckoutCommandHandler_WhenProductDeleted_Fails()
    {
        var context = await BuildCheckoutAsync();
        _productRepository.Remove(context.Product);

        var result = await context.Handler.Handle(
            new CheckoutCommand(context.DeliveryMethod.Id.Value, context.UserAddress.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.ProductNotFound.Code, result.Error.Code);
        Assert.Empty(_orderRepository.Items);
    }

    [Fact]
    public async Task CheckoutCommandHandler_WithForeignAddress_Fails()
    {
        var foreignUserId = Guid.NewGuid();
        var context = await BuildCheckoutAsync(addressOwnerId: foreignUserId);

        var result = await context.Handler.Handle(
            new CheckoutCommand(context.DeliveryMethod.Id.Value, context.UserAddress.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserAddressErrors.NotFound.Code, result.Error.Code);
        Assert.Empty(_orderRepository.Items);
    }

    [Fact]
    public async Task CheckoutCommandHandler_WithUnknownDeliveryMethod_Fails()
    {
        var context = await BuildCheckoutAsync();

        var result = await context.Handler.Handle(
            new CheckoutCommand(Guid.NewGuid(), context.UserAddress.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DeliveryMethodErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task CheckoutCommandHandler_WithUnavailableDeliveryMethod_Fails()
    {
        var context = await BuildCheckoutAsync();
        context.DeliveryMethod.ChangeAvailability(false);

        var result = await context.Handler.Handle(
            new CheckoutCommand(context.DeliveryMethod.Id.Value, context.UserAddress.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DeliveryMethodErrors.NotAvailable.Code, result.Error.Code);
        Assert.Empty(_orderRepository.Items);
    }

    [Fact]
    public async Task CheckoutCommandHandler_WithEmptyBasket_Fails()
    {
        var context = await BuildCheckoutAsync(seedBasketItem: false);

        var result = await context.Handler.Handle(
            new CheckoutCommand(context.DeliveryMethod.Id.Value, context.UserAddress.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.NoOrderItems.Code, result.Error.Code);
    }

    [Fact]
    public async Task CheckoutCommandHandler_WithoutUser_Fails()
    {
        var context = await BuildCheckoutAsync();
        _currentUser.UserId = null;

        var result = await context.Handler.Handle(
            new CheckoutCommand(context.DeliveryMethod.Id.Value, context.UserAddress.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.InvalidUserId.Code, result.Error.Code);
    }

    [Fact]
    public async Task CheckoutCommandHandler_WhenIntentCreationFails_CancelsOrder()
    {
        var context = await BuildCheckoutAsync();
        _payment.FailIntentCreation = true;

        var result = await context.Handler.Handle(
            new CheckoutCommand(context.DeliveryMethod.Id.Value, context.UserAddress.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.StripeCreateFailed", result.Error.Code);

        var order = _orderRepository.Items.Single();
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal("Payment initialization failed", order.CancellationReason);
    }

    [Fact]
    public async Task HandlePaymentSucceededCommand_PaidOrder_TransitionsToProcessing()
    {
        var order = await SeedPaidOrderAsync();

        var handler = new HandlePaymentSucceededCommandHandler(
            _orderRepository, _unitOfWork, NullLogger<HandlePaymentSucceededCommandHandler>.Instance);

        var result = await handler.Handle(
            new HandlePaymentSucceededCommand(order.PaymentIntentId!),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var stored = await _orderRepository.GetByIdAsync(order.Id);
        Assert.Equal(OrderStatus.Processing, stored!.Status);
        Assert.NotNull(stored.PaidAtUtc);
    }

    [Fact]
    public async Task HandlePaymentSucceededCommand_Replayed_IsIdempotent()
    {
        var order = await SeedPaidOrderAsync();

        var handler = new HandlePaymentSucceededCommandHandler(
            _orderRepository, _unitOfWork, NullLogger<HandlePaymentSucceededCommandHandler>.Instance);

        Assert.True(
            (await handler.Handle(new HandlePaymentSucceededCommand(order.PaymentIntentId!), CancellationToken.None)).IsSuccess);

        var second = await handler.Handle(
            new HandlePaymentSucceededCommand(order.PaymentIntentId!), CancellationToken.None);

        Assert.True(second.IsSuccess);
        var stored = await _orderRepository.GetByIdAsync(order.Id);
        Assert.Equal(OrderStatus.Processing, stored!.Status);
        Assert.Equal(1, stored.GetDomainEvents().Count(e => e is OrderPaidDomainEvent));
    }

    [Fact]
    public async Task HandlePaymentFailedCommand_WhenAlreadyPaid_NoOps()
    {
        var order = await SeedPaidOrderAsync();
        var paidAt = order.PaidAtUtc;

        var handler = new HandlePaymentFailedCommandHandler(
            _orderRepository, _unitOfWork, NullLogger<HandlePaymentFailedCommandHandler>.Instance);

        var result = await handler.Handle(
            new HandlePaymentFailedCommand(order.PaymentIntentId!),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var stored = await _orderRepository.GetByIdAsync(order.Id);
        Assert.Equal(OrderStatus.Processing, stored!.Status);
        Assert.Equal(paidAt, stored.PaidAtUtc);
    }

    [Fact]
    public async Task HandleRefundedCommand_PaidOrder_TransitionsToRefunded()
    {
        var order = await SeedPaidOrderAsync();

        var handler = new HandleRefundedCommandHandler(
            _orderRepository, _unitOfWork, NullLogger<HandleRefundedCommandHandler>.Instance);

        var result = await handler.Handle(
            new HandleRefundedCommand(order.PaymentIntentId!),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var stored = await _orderRepository.GetByIdAsync(order.Id);
        Assert.Equal(OrderStatus.Refunded, stored!.Status);
        Assert.NotNull(stored.RefundedAtUtc);
    }

    [Fact]
    public async Task HandleStripeWebhookCommand_SucceededEvent_MarksOrderPaid()
    {
        var order = await SeedPendingOrderAsync("pi_webhook");
        var handler = BuildWebhookHandler(new PaymentWebhookOutcome(
            PaymentOutcome.Succeeded, "pi_webhook", null, "evt_1", "payment_intent.succeeded"));

        var result = await handler.Handle(
            new HandleStripeWebhookCommand("{}", "t=1,v1=sig"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Processed);
        Assert.Equal("Succeeded", result.Value.Outcome);

        var stored = await _orderRepository.GetByIdAsync(order.Id);
        Assert.Equal(OrderStatus.Processing, stored!.Status);
    }

    [Fact]
    public async Task HandleStripeWebhookCommand_FailedEvent_CancelsPendingOrder()
    {
        var order = await SeedPendingOrderAsync("pi_failed");
        var handler = BuildWebhookHandler(new PaymentWebhookOutcome(
            PaymentOutcome.Failed, "pi_failed", "card declined", "evt_2", "payment_intent.payment_failed"));

        var result = await handler.Handle(
            new HandleStripeWebhookCommand("{}", "t=1,v1=sig"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var stored = await _orderRepository.GetByIdAsync(order.Id);
        Assert.Equal(OrderStatus.Cancelled, stored!.Status);
    }

    [Fact]
    public async Task HandleStripeWebhookCommand_RefundedEvent_MarksOrderRefunded()
    {
        var order = await SeedPaidOrderAsync();
        order.ShipOrder();
        var handler = BuildWebhookHandler(new PaymentWebhookOutcome(
            PaymentOutcome.Refunded, order.PaymentIntentId, null, "evt_3", "payment_intent.refunded"));

        var result = await handler.Handle(
            new HandleStripeWebhookCommand("{}", "t=1,v1=sig"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var stored = await _orderRepository.GetByIdAsync(order.Id);
        Assert.Equal(OrderStatus.Refunded, stored!.Status);
    }

    [Fact]
    public async Task HandleStripeWebhookCommand_UnhandledEvent_IsProcessedAsNoOp()
    {
        var order = await SeedPendingOrderAsync("pi_other");
        var handler = BuildWebhookHandler(new PaymentWebhookOutcome(
            PaymentOutcome.Unhandled, null, null, "evt_4", "charge.refunded"));

        var result = await handler.Handle(
            new HandleStripeWebhookCommand("{}", "t=1,v1=sig"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.Processed);
        var stored = await _orderRepository.GetByIdAsync(order.Id);
        Assert.Equal(OrderStatus.Pending, stored!.Status);
    }

    [Fact]
    public async Task HandleStripeWebhookCommand_InvalidSignature_Fails()
    {
        _ = await SeedPendingOrderAsync("pi_invalid");
        var handler = BuildWebhookHandler(null);

        var result = await handler.Handle(
            new HandleStripeWebhookCommand("{}", ""),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrors.InvalidSignature.Code, result.Error.Code);
    }

    [Fact]
    public async Task ShipOrderCommandHandler_TransitionsPaidOrderToShipped()
    {
        var paid = await SeedPaidOrderAsync();

        var handler = new ShipOrderCommandHandler(_orderRepository, _unitOfWork);

        var result = await handler.Handle(new ShipOrderCommand(paid.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var stored = await _orderRepository.GetByIdAsync(paid.Id);
        Assert.Equal(OrderStatus.Shipped, stored!.Status);
        Assert.NotNull(stored.ShippedAtUtc);
    }

    [Fact]
    public async Task DeliverOrderCommandHandler_TransitionsShippedOrderToDelivered()
    {
        var paid = await SeedPaidOrderAsync();
        Assert.True(paid.ShipOrder().IsSuccess);

        var handler = new DeliverOrderCommandHandler(_orderRepository, _unitOfWork);

        var result = await handler.Handle(new DeliverOrderCommand(paid.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var stored = await _orderRepository.GetByIdAsync(paid.Id);
        Assert.Equal(OrderStatus.Delivered, stored!.Status);
    }

    [Fact]
    public async Task ShipOrderCommandHandler_PendingOrder_Fails()
    {
        var pending = await SeedPendingOrderAsync();

        var handler = new ShipOrderCommandHandler(_orderRepository, _unitOfWork);

        var result = await handler.Handle(new ShipOrderCommand(pending.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.InvalidStateTransition.Code, result.Error.Code);
    }

    [Fact]
    public async Task ShipOrderCommandHandler_UnknownOrder_Fails()
    {
        var handler = new ShipOrderCommandHandler(_orderRepository, _unitOfWork);

        var result = await handler.Handle(new ShipOrderCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task CancelOrderCommandHandler_AdminCancelsPaidOrder()
    {
        var paid = await SeedPaidOrderAsync();

        var handler = new CancelOrderCommandHandler(_orderRepository, _unitOfWork);

        var result = await handler.Handle(new CancelOrderCommand(paid.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var stored = await _orderRepository.GetByIdAsync(paid.Id);
        Assert.Equal(OrderStatus.Cancelled, stored!.Status);
        Assert.Equal("Cancelled by admin", stored.CancellationReason);
    }

    [Fact]
    public async Task CancelOwnOrderCommandHandler_OwnPendingOrder_CancelsWithReason()
    {
        var order = await SeedPendingOrderAsync();

        var handler = new CancelOwnOrderCommandHandler(_orderRepository, _currentUser, _unitOfWork);

        var result = await handler.Handle(new CancelOwnOrderCommand(order.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var stored = await _orderRepository.GetByIdAsync(order.Id);
        Assert.Equal(OrderStatus.Cancelled, stored!.Status);
        Assert.Equal("Cancelled by customer", stored.CancellationReason);
    }

    [Fact]
    public async Task CancelOwnOrderCommandHandler_ForeignUserOrder_IsNotFound()
    {
        var order = await SeedPendingOrderAsync(userId: Guid.NewGuid());

        var handler = new CancelOwnOrderCommandHandler(_orderRepository, _currentUser, _unitOfWork);

        var result = await handler.Handle(new CancelOwnOrderCommand(order.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task CancelOwnOrderCommandHandler_PaidOrder_Fails()
    {
        var order = await SeedPaidOrderAsync();

        var handler = new CancelOwnOrderCommandHandler(_orderRepository, _currentUser, _unitOfWork);

        var result = await handler.Handle(new CancelOwnOrderCommand(order.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.InvalidStateTransition.Code, result.Error.Code);
    }

    [Fact]
    public async Task GetOrderByIdQueryHandler_ReturnsOnlyOwnOrder()
    {
        var own = await SeedPaidOrderAsync();
        var foreignUser = Guid.NewGuid();
        var foreign = await SeedPendingOrderAsync(userId: foreignUser);
        _db.OrdersList.Add(own);
        _db.OrdersList.Add(foreign);

        var handler = new GetOrderByIdQueryHandler(_db, _currentUser);

        var ownResponse = await handler.Handle(new GetOrderByIdQuery(own.Id.Value), CancellationToken.None);
        var foreignResponse = await handler.Handle(new GetOrderByIdQuery(foreign.Id.Value), CancellationToken.None);

        Assert.NotNull(ownResponse);
        Assert.Equal(OrderStatus.Processing.ToString(), ownResponse!.Status);
        Assert.Equal(_currentUser.UserId!.Value, ownResponse!.UserId);
        Assert.Single(ownResponse.Items);

        Assert.Null(foreignResponse);
    }

    [Fact]
    public async Task GetOrdersQueryHandler_FiltersByUserAndStatus()
    {
        var ownPaid = await SeedPaidOrderAsync();
        var ownPending = await SeedPendingOrderAsync();
        var foreign = await SeedPendingOrderAsync(userId: Guid.NewGuid());
        _db.OrdersList.AddRange(ownPaid, ownPending, foreign);

        var handler = new GetOrdersQueryHandler(_db, _currentUser);

        var allMine = await handler.Handle(new GetOrdersQuery(), CancellationToken.None);
        Assert.Equal(2, allMine.TotalCount);
        Assert.Equal(2, allMine.Items.Count);

        var pendingOnly = await handler.Handle(new GetOrdersQuery(Status: OrderStatus.Pending), CancellationToken.None);
        Assert.Equal(1, pendingOnly.TotalCount);
        Assert.Equal(ownPending.Id.Value, pendingOnly.Items.Single().Id);
    }

    [Fact]
    public async Task GetOrdersQueryHandler_WithoutUser_ReturnsEmptyPage()
    {
        _currentUser.UserId = null;
        var handler = new GetOrdersQueryHandler(_db, _currentUser);

        var page = await handler.Handle(new GetOrdersQuery(), CancellationToken.None);

        Assert.Equal(0, page.TotalCount);
        Assert.Empty(page.Items);
    }

    private async Task<CheckoutContext> BuildCheckoutAsync(
        bool seedBasketItem = true,
        decimal productPrice = 20m,
        Guid? addressOwnerId = null)
    {
        var userId = _currentUser.UserId!.Value;

        var product = Product(productPrice);
        _productRepository.Add(product);

        var delivery = TestDelivery();
        _deliveryMethodRepository.Add(delivery);

        var ownerId = addressOwnerId ?? userId;
        var userAddress = UserAddress.Create(
            UserAddressId.New(),
            ownerId,
            "Home",
            Address.Create("John", "Doe", "+20 100 000 0000", "Egypt", "Cairo", "1 Main Street", "11511").Value).Value;
        _db.UserAddressesList.Add(userAddress);

        if (seedBasketItem)
        {
            var basket = await _basketStore.GetOrCreateAsync(userId);
            _ = basket.AddItem(product.Id, product.Name, product.PictureUrl, product.Price, 1);
            await _basketStore.SaveAsync(basket);
        }

        var handler = new CheckoutCommandHandler(
            _orderRepository,
            _deliveryMethodRepository,
            _productRepository,
            _db,
            _basketStore,
            _currentUser,
            _payment,
            _unitOfWork);

        return new CheckoutContext(product, delivery, userAddress, handler, userId);
    }

    private static Product Product(decimal amount = 20m)
        => Domain.Entities.Products.Product.Create(ProductId.New(), "Classic Tee", "A comfortable t-shirt.", "https://img.example.com/tee.png", Money.Create(amount, Currency.Usd).Value).Value;

    private static DeliveryMethod TestDelivery(decimal fee = 5m)
        => DeliveryMethod.Create(
            DeliveryMethodId.New(),
            "Standard",
            "3-5 business days",
            Money.Create(fee, Currency.Usd).Value).Value;

    private async Task<Order> SeedPendingOrderAsync(string intentId = "pi_test_intent", Guid? userId = null)
    {
        var product = Product();
        _productRepository.Add(product);

        var order = Order.Create(
            OrderId.New(),
            userId ?? _currentUser.UserId!.Value,
            Address.Create("John", "Doe", "+20 100 000 0000", "Egypt", "Cairo", "1 Main Street", "11511").Value,
            [new BasketItemDto(product.Id, product.Name, product.PictureUrl, Money.Create(20m, Currency.Usd).Value, 1)],
            OrderDeliveryMethod.FromEntity(TestDelivery())).Value;

        Assert.True(order.SetPaymentIntentId(intentId).IsSuccess);
        _orderRepository.Add(order);

        return order;
    }

    private async Task<Order> SeedPaidOrderAsync()
    {
        var order = await SeedPendingOrderAsync();
        Assert.True(order.MarkAsPaid(order.PaymentIntentId!).IsSuccess);
        return order;
    }

    private HandleStripeWebhookCommandHandler BuildWebhookHandler(PaymentWebhookOutcome? outcome)
    {
        _payment.NextOutcome = outcome;

        var services = new ServiceCollection();
        services.AddApplication();
        services.AddSingleton<IUnitOfWork>(_unitOfWork);
        services.AddSingleton<IOrderRepository>(_orderRepository);
        services.AddSingleton<IPaymentService>(_payment);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

        var provider = services.BuildServiceProvider();

        return new HandleStripeWebhookCommandHandler(
            _payment,
            provider.GetRequiredService<ISender>(),
            NullLogger<HandleStripeWebhookCommandHandler>.Instance);
    }

    private sealed record CheckoutContext(
        Product Product,
        DeliveryMethod DeliveryMethod,
        UserAddress UserAddress,
        CheckoutCommandHandler Handler,
        Guid UserId);

    private sealed class FakeDbContext : IApplicationDbContext
    {
        public readonly List<UserAddress> UserAddressesList = [];
        public readonly List<Order> OrdersList = [];

        public IQueryable<UserAddress> UserAddresses => UserAddressesList.AsQueryable().AsTestAsyncQueryable();
        public IQueryable<Order> Orders => OrdersList.AsQueryable().AsTestAsyncQueryable();
        public IQueryable<Product> Products => Array.Empty<Product>().AsQueryable().AsTestAsyncQueryable();
        public IQueryable<ProductBrand> Brands => Array.Empty<ProductBrand>().AsQueryable().AsTestAsyncQueryable();
        public IQueryable<ProductType> Types => Array.Empty<ProductType>().AsQueryable().AsTestAsyncQueryable();
        public IQueryable<DeliveryMethod> DeliveryMethods => Array.Empty<DeliveryMethod>().AsQueryable().AsTestAsyncQueryable();

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? UserId { get; set; }
        public string? Email { get; set; }
        public bool IsAuthenticated => UserId.HasValue;
    }

    private sealed class FakeOrderRepository : IOrderRepository
    {
        private readonly Dictionary<OrderId, Order> _orders = [];

        public IReadOnlyCollection<Order> Items => _orders.Values;

        public Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default)
        {
            _orders.TryGetValue(id, out var order);
            return Task.FromResult(order);
        }

        public Task<Order?> GetByPaymentIntentIdAsync(string paymentIntentId, CancellationToken ct = default)
            => Task.FromResult(_orders.Values.FirstOrDefault(o => o.PaymentIntentId == paymentIntentId));

        public Task<IReadOnlyList<Order>> GetPendingByUserAsync(Guid userId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Order>>(
                _orders.Values.Where(o => o.UserId == userId && o.Status == OrderStatus.Pending).ToList());

        public void Add(Order order) => _orders[order.Id] = order;
    }

    private sealed class FakeDeliveryMethodRepository : IDeliveryMethodRepository
    {
        private readonly Dictionary<DeliveryMethodId, DeliveryMethod> _deliveryMethods = [];

        public Task<DeliveryMethod?> GetByIdAsync(DeliveryMethodId id, CancellationToken ct = default)
        {
            _deliveryMethods.TryGetValue(id, out var deliveryMethod);
            return Task.FromResult(deliveryMethod);
        }

        public void Add(DeliveryMethod deliveryMethod) => _deliveryMethods[deliveryMethod.Id] = deliveryMethod;
        public void Remove(DeliveryMethod deliveryMethod) => _deliveryMethods.Remove(deliveryMethod.Id);
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        private readonly Dictionary<ProductId, Product> _products = [];

        public IReadOnlyCollection<Product> Items => _products.Values;

        public Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default)
        {
            _products.TryGetValue(id, out var product);
            return Task.FromResult(product);
        }

        public void Add(Product product) => _products[product.Id] = product;
        public void Remove(Product product) => _products.Remove(product.Id);
    }

    private sealed class FakeBasketStore : IBasketStore
    {
        private readonly Dictionary<Guid, Basket> _baskets = [];

        public Task<Basket?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            _baskets.TryGetValue(userId, out var basket);
            return Task.FromResult(basket);
        }

        public Task<Basket> GetOrCreateAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            if (!_baskets.TryGetValue(userId, out var basket))
            {
                basket = Basket.Create(userId).Value;
                _baskets[userId] = basket;
            }
            return Task.FromResult(basket);
        }

        public Task SaveAsync(Basket basket, CancellationToken cancellationToken = default)
        {
            _baskets[basket.UserId] = basket;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            _baskets.Remove(userId);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePaymentService : IPaymentService
    {
        private int _intentCounter;

        public List<string> CancelledIntents { get; } = [];
        public PaymentWebhookOutcome? NextOutcome { get; set; }
        public bool FailIntentCreation { get; set; }

        public Task<Result<PaymentIntentResult>> CreatePaymentIntentAsync(Order order, CancellationToken ct = default)
            => Task.FromResult(FailIntentCreation
                ? Result.Failure<PaymentIntentResult>(Error.Problem("Payment.StripeCreateFailed", "stripe is down"))
                : Result.Success(new PaymentIntentResult(
                    $"pi_{++_intentCounter}",
                    $"cs_test_{_intentCounter}",
                    (long)(order.Total.Amount * 100),
                    order.Total.Currency.Code.ToLowerInvariant())));

        public Task CancelPaymentIntentAsync(string paymentIntentId, CancellationToken ct = default)
        {
            CancelledIntents.Add(paymentIntentId);
            return Task.CompletedTask;
        }

        public Task<Result<PaymentWebhookOutcome>> HandleWebhookAsync(string json, string signatureHeader, CancellationToken ct = default)
            => Task.FromResult(NextOutcome is not null
                ? Result.Success(NextOutcome)
                : Result.Failure<PaymentWebhookOutcome>(PaymentErrors.InvalidSignature));
    }
}
