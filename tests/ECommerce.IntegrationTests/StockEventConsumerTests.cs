using ECommerce.Application.Common;
using ECommerce.Application.Common.EventHandlers;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Inventory;
using ECommerce.Application.Inventory.EventHandlers;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Inventory;
using ECommerce.Domain.Entities.Inventory.Events;
using ECommerce.Domain.Entities.Orders;
using ECommerce.Domain.Entities.Products;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ECommerce.IntegrationTests;

public class StockEventConsumerTests
{
    private readonly FakeInbox _inbox = new(claimSucceeds: true);
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeOutboxMessageContext _messageContext = new();
    private readonly FakeOutboxWriter _outboxWriter = new();
    private readonly FakeInventoryItemRepository _inventory = new();

    [Fact]
    public async Task StockReservedConsumer_WhenProductDepleted_EnqueuesDepletionFollowUp()
    {
        var inventoryItem = InventoryItem.Create(ProductId.New(), 2).Value;
        // Simulates the aggregate having already dropped to zero.
        var orderId = OrderId.New();
        Assert.True(inventoryItem.Reserve(orderId, 2).IsSuccess);
        _inventory.Add(inventoryItem);

        var handler = CreateReservedHandler();

        await handler.Handle(
            new StockReservedDomainEvent(inventoryItem.Id, inventoryItem.ProductId, orderId, 2, 0),
            CancellationToken.None);

        var followUp = Assert.Single(_outboxWriter.Messages);
        var depleted = Assert.IsType<StockDepletedNotification>(followUp);
        Assert.Equal(inventoryItem.Id.Value, depleted.InventoryItemId);
        Assert.Equal(inventoryItem.ProductId.Value, depleted.ProductId);
    }

    [Fact]
    public async Task StockReservedConsumer_WhenStockStillAvailable_NoDepletionFollowUp()
    {
        var inventoryItem = InventoryItem.Create(ProductId.New(), 10).Value;
        _inventory.Add(inventoryItem);

        var handler = CreateReservedHandler();

        await handler.Handle(
            new StockReservedDomainEvent(inventoryItem.Id, inventoryItem.ProductId, OrderId.New(), 3, 7),
            CancellationToken.None);

        Assert.Empty(_outboxWriter.Messages);
    }

    [Fact]
    public async Task StockReservedConsumer_WhenZeroEventIsStaleHistory_SkipsDepletionFollowUp()
    {
        // The event says zero, but the item has been topped up since: stale history.
        var inventoryItem = InventoryItem.Create(ProductId.New(), 10).Value;
        _inventory.Add(inventoryItem);

        var handler = CreateReservedHandler();

        await handler.Handle(
            new StockReservedDomainEvent(inventoryItem.Id, inventoryItem.ProductId, OrderId.New(), 2, 0),
            CancellationToken.None);

        Assert.Empty(_outboxWriter.Messages);
    }

    [Fact]
    public async Task StockAdjustedConsumer_WhenAdjustmentDrivesToZero_EnqueuesDepletionFollowUp()
    {
        var inventoryItem = InventoryItem.Create(ProductId.New(), 4).Value;
        // The aggregate is genuinely depleted, as the adjust that raised the event did.
        Assert.True(inventoryItem.Adjust(-4).IsSuccess);
        _inventory.Add(inventoryItem);

        var handler = new StockAdjustedDomainEventHandler(
            _inbox, _unitOfWork, _messageContext, _inventory, _outboxWriter,
            NullLogger<StockAdjustedDomainEventHandler>.Instance);

        await handler.Handle(
            new StockAdjustedDomainEvent(inventoryItem.Id, inventoryItem.ProductId, -4, 0),
            CancellationToken.None);

        var followUp = Assert.Single(_outboxWriter.Messages);
        Assert.IsType<StockDepletedNotification>(followUp);
    }

    [Fact]
    public async Task StockAdjustedConsumer_WhenAdjustmentIsNotADepletion_NoFollowUp()
    {
        var inventoryItem = InventoryItem.Create(ProductId.New(), 4).Value;
        _inventory.Add(inventoryItem);

        var handler = new StockAdjustedDomainEventHandler(
            _inbox, _unitOfWork, _messageContext, _inventory, _outboxWriter,
            NullLogger<StockAdjustedDomainEventHandler>.Instance);

        await handler.Handle(
            new StockAdjustedDomainEvent(inventoryItem.Id, inventoryItem.ProductId, 3, 7),
            CancellationToken.None);

        Assert.Empty(_outboxWriter.Messages);
    }

    [Fact]
    public async Task StockReleasedConsumer_FreesStockOnly_NeverEnqueuesFollowUps()
    {
        var handler = new StockReleasedDomainEventHandler(
            _inbox, _unitOfWork, _messageContext, NullLogger<StockReleasedDomainEventHandler>.Instance);

        await handler.Handle(
            new StockReleasedDomainEvent(InventoryItemId.New(), ProductId.New(), OrderId.New(), 4, 10),
            CancellationToken.None);

        Assert.Empty(_outboxWriter.Messages);
    }

    [Fact]
    public async Task StockConsumer_WhenDuplicateClaim_SuppressedWithoutWork()
    {
        var inbox = new FakeInbox(claimSucceeds: false);
        var reservedHandler = new StockReservedDomainEventHandler(
            inbox, _unitOfWork, _messageContext, _inventory, _outboxWriter,
            NullLogger<StockReservedDomainEventHandler>.Instance);

        await reservedHandler.Handle(
            new StockReservedDomainEvent(InventoryItemId.New(), ProductId.New(), OrderId.New(), 3, 0),
            CancellationToken.None);

        Assert.Equal(1, inbox.ClaimAttempts);
        Assert.Empty(_outboxWriter.Messages);
    }

    [Fact]
    public async Task StockDepletedConsumer_ClaimsUnderItsOwnConsumerId()
    {
        var handler = new StockDepletedNotificationHandler(
            _inbox, _unitOfWork, _messageContext, NullLogger<StockDepletedNotificationHandler>.Instance);

        // Two distinct messages: each is claimed once for this consumer.
        await handler.Handle(new StockDepletedNotification(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);
        await handler.Handle(new StockDepletedNotification(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(2, _inbox.TotalClaimsFor("inventory.stock-depleted"));
        Assert.Empty(_outboxWriter.Messages); // a terminal consumer enqueues no follow-ups itself
    }

    private StockReservedDomainEventHandler CreateReservedHandler()
        => new(
            _inbox, _unitOfWork, _messageContext, _inventory, _outboxWriter,
            NullLogger<StockReservedDomainEventHandler>.Instance);

    private sealed class FakeInbox : IInbox
    {
        private readonly bool _claimSucceeds;
        private readonly List<string> _claimsFor = [];

        public FakeInbox(bool claimSucceeds) => _claimSucceeds = claimSucceeds;

        public int ClaimAttempts { get; private set; }

        public Task<bool> TryClaimAsync(Guid messageId, string eventType, string consumer, CancellationToken ct = default)
        {
            ClaimAttempts++;
            if (_claimSucceeds)
            {
                _claimsFor.Add(consumer);
            }

            return Task.FromResult(_claimSucceeds);
        }

        public int TotalClaimsFor(string consumer)
            => _claimsFor.Count(c => c == consumer);
    }

    private sealed class FakeOutboxMessageContext : IOutboxMessageContext
    {
        public Guid MessageId { get; } = Guid.NewGuid();

        public string EventType { get; } = "StockReservedDomainEvent";

        public bool IsAvailable => true;
    }

    private sealed class FakeOutboxWriter : IOutboxWriter
    {
        public List<INotification> Messages { get; } = [];

        public void Enqueue(INotification message) => Messages.Add(message);
    }

    private sealed class FakeInventoryItemRepository : IInventoryItemRepository
    {
        private readonly Dictionary<ProductId, InventoryItem> _items = [];

        public IReadOnlyCollection<InventoryItem> Items => _items.Values;

        public Task<InventoryItem?> GetByProductIdAsync(ProductId productId, CancellationToken ct = default)
        {
            _items.TryGetValue(productId, out var item);
            return Task.FromResult(item);
        }

        public Task<List<InventoryItem>> GetByProductIdsAsync(IReadOnlyCollection<ProductId> productIds, CancellationToken ct = default)
            => Task.FromResult(_items.Values.Where(i => productIds.Contains(i.ProductId)).ToList());

        public void Add(InventoryItem inventoryItem) => _items[inventoryItem.ProductId] = inventoryItem;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        // Runs the consumer's work inline; DB transactions are not under test here.
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);

        public Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> action,
            CancellationToken cancellationToken = default)
            => action(cancellationToken);
    }
}
