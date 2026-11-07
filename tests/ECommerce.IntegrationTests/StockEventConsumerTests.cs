using ECommerce.Application.Inventory;
using ECommerce.Application.Inventory.EventHandlers;
using ECommerce.Domain.Entities.Inventory;
using ECommerce.Domain.Entities.Inventory.Events;
using ECommerce.Domain.Entities.Orders;
using ECommerce.Domain.Entities.Products;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ECommerce.IntegrationTests;

public class StockEventConsumerTests
{
    private readonly FakeInbox _inbox = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeOutboxMessageContext _messageContext = new();
    private readonly FakeOutboxWriter _outboxWriter = new();
    private readonly FakeInventoryItemRepository _inventory = new();
    private readonly FakeStockAttentionPolicy _policy = new(lowStockThreshold: 5);

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
    public async Task StockReservedConsumer_WhenAtLowStockThreshold_EnqueuesLowStockFollowUp()
    {
        // Threshold 5: exactly at the threshold counts as low stock.
        var inventoryItem = InventoryItem.Create(ProductId.New(), 10).Value;
        var orderId = OrderId.New();
        Assert.True(inventoryItem.Reserve(orderId, 5).IsSuccess); // 10 -> 5
        _inventory.Add(inventoryItem);

        var handler = CreateReservedHandler();

        await handler.Handle(
            new StockReservedDomainEvent(inventoryItem.Id, inventoryItem.ProductId, orderId, 5, 5),
            CancellationToken.None);

        var followUp = Assert.Single(_outboxWriter.Messages);
        var low = Assert.IsType<StockLowNotification>(followUp);
        Assert.Equal(5, low.AvailableQuantity);
        Assert.Equal(inventoryItem.ProductId.Value, low.ProductId);
    }

    [Fact]
    public async Task StockReservedConsumer_WhenAboveLowStockThreshold_NoFollowUp()
    {
        var inventoryItem = InventoryItem.Create(ProductId.New(), 10).Value;
        var orderId = OrderId.New();
        Assert.True(inventoryItem.Reserve(orderId, 4).IsSuccess); // 10 -> 6
        _inventory.Add(inventoryItem);

        var handler = CreateReservedHandler();

        await handler.Handle(
            new StockReservedDomainEvent(inventoryItem.Id, inventoryItem.ProductId, orderId, 4, 6),
            CancellationToken.None);

        Assert.Empty(_outboxWriter.Messages);
    }

    [Fact]
    public async Task StockReservedConsumer_WhenLowStockAlertsDisabled_NoLowStockFollowUp()
    {
        var inventoryItem = InventoryItem.Create(ProductId.New(), 10).Value;
        var orderId = OrderId.New();
        Assert.True(inventoryItem.Reserve(orderId, 7).IsSuccess); // 10 -> 3, below threshold
        _inventory.Add(inventoryItem);

        var handler = new StockReservedDomainEventHandler(
            _inbox, _unitOfWork, _messageContext, _inventory,
            new FakeStockAttentionPolicy(lowStockAlertsEnabled: false), _outboxWriter,
            NullLogger<StockReservedDomainEventHandler>.Instance);

        await handler.Handle(
            new StockReservedDomainEvent(inventoryItem.Id, inventoryItem.ProductId, orderId, 7, 3),
            CancellationToken.None);

        Assert.Empty(_outboxWriter.Messages);
    }

    [Fact]
    public async Task StockReservedConsumer_WhenLowStockEventIsStaleHistory_SkipsLowStockFollowUp()
    {
        // Event says 4 (below threshold 5), but stock has since recovered to 10.
        var inventoryItem = InventoryItem.Create(ProductId.New(), 10).Value;
        _inventory.Add(inventoryItem);

        var handler = CreateReservedHandler();

        await handler.Handle(
            new StockReservedDomainEvent(inventoryItem.Id, inventoryItem.ProductId, OrderId.New(), 6, 4),
            CancellationToken.None);

        Assert.Empty(_outboxWriter.Messages);
    }

    [Fact]
    public async Task StockAdjustedConsumer_WhenAdjustmentDrivesToZero_EnqueuesOnlyDepletionFollowUp()
    {
        var inventoryItem = InventoryItem.Create(ProductId.New(), 4).Value;
        // The aggregate is genuinely depleted, as the adjust that raised the event did.
        Assert.True(inventoryItem.Adjust(-4).IsSuccess);
        _inventory.Add(inventoryItem);

        var handler = CreateAdjustedHandler();

        await handler.Handle(
            new StockAdjustedDomainEvent(inventoryItem.Id, inventoryItem.ProductId, -4, 0),
            CancellationToken.None);

        var followUp = Assert.Single(_outboxWriter.Messages);
        Assert.IsType<StockDepletedNotification>(followUp);
    }

    [Fact]
    public async Task StockAdjustedConsumer_WhenAdjustmentBreachesThreshold_EnqueuesLowStockFollowUp()
    {
        var inventoryItem = InventoryItem.Create(ProductId.New(), 12).Value;
        Assert.True(inventoryItem.Adjust(-7).IsSuccess); // 12 -> 5, at threshold
        _inventory.Add(inventoryItem);

        var handler = CreateAdjustedHandler();

        await handler.Handle(
            new StockAdjustedDomainEvent(inventoryItem.Id, inventoryItem.ProductId, -7, 5),
            CancellationToken.None);

        var followUp = Assert.Single(_outboxWriter.Messages);
        Assert.IsType<StockLowNotification>(followUp);
    }

    [Fact]
    public async Task StockAdjustedConsumer_WhenAdjustmentIsNotADepletion_NoFollowUp()
    {
        var inventoryItem = InventoryItem.Create(ProductId.New(), 4).Value;
        _inventory.Add(inventoryItem);

        var handler = CreateAdjustedHandler();

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
    public async Task StockConsumers_WhenDuplicateClaim_SuppressedWithoutWork()
    {
        var inbox = new FakeInbox(claimSucceeds: false);
        var reservedHandler = new StockReservedDomainEventHandler(
            inbox, _unitOfWork, _messageContext, _inventory, _policy, _outboxWriter,
            NullLogger<StockReservedDomainEventHandler>.Instance);

        await reservedHandler.Handle(
            new StockReservedDomainEvent(InventoryItemId.New(), ProductId.New(), OrderId.New(), 3, 0),
            CancellationToken.None);

        Assert.Equal(1, inbox.ClaimAttempts);
        Assert.Empty(_outboxWriter.Messages);
    }

    [Fact]
    public async Task StockLowConsumer_ClaimsUnderItsOwnConsumerId()
    {
        var handler = new StockLowNotificationHandler(
            _inbox, _unitOfWork, _messageContext, NullLogger<StockLowNotificationHandler>.Instance);

        await handler.Handle(new StockLowNotification(Guid.NewGuid(), Guid.NewGuid(), 3), CancellationToken.None);
        await handler.Handle(new StockLowNotification(Guid.NewGuid(), Guid.NewGuid(), 4), CancellationToken.None);

        Assert.Equal(2, _inbox.TotalClaimsFor("inventory.stock-low"));
        Assert.Empty(_outboxWriter.Messages); // terminal consumer enqueues no follow-ups itself
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
        Assert.Empty(_outboxWriter.Messages); // terminal consumer enqueues no follow-ups itself
    }

    private StockReservedDomainEventHandler CreateReservedHandler()
        => new(
            _inbox, _unitOfWork, _messageContext, _inventory, _policy, _outboxWriter,
            NullLogger<StockReservedDomainEventHandler>.Instance);

    private StockAdjustedDomainEventHandler CreateAdjustedHandler()
        => new(
            _inbox, _unitOfWork, _messageContext, _inventory, _policy, _outboxWriter,
            NullLogger<StockAdjustedDomainEventHandler>.Instance);
}
