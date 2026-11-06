using ECommerce.Domain.Entities.Inventory;
using ECommerce.Domain.Entities.Inventory.Events;
using ECommerce.Domain.Entities.Orders;
using ECommerce.Domain.Entities.Products;
using Xunit;

namespace ECommerce.Domain.UnitTests;

public class InventoryItemTests
{
    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var result = InventoryItem.Create(ProductId.New(), 10);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value.AvailableQuantity);
    }

    [Fact]
    public void Create_WithQuantity_WritesInitialLedgerEntry()
    {
        var inventoryItem = InventoryItem.Create(ProductId.New(), 10).Value;

        var entry = Assert.Single(inventoryItem.Ledger);
        Assert.Equal(StockLedgerEntryType.Initial, entry.Type);
        Assert.Equal(10, entry.Quantity);
        Assert.Null(entry.OrderId);
    }

    [Fact]
    public void Create_WithZeroQuantity_WritesNoLedgerEntries()
    {
        var inventoryItem = InventoryItem.Create(ProductId.New(), 0).Value;

        Assert.Empty(inventoryItem.Ledger);
    }

    [Fact]
    public void Create_WithEmptyProductId_Fails()
    {
        var result = InventoryItem.Create(default, 10);

        Assert.True(result.IsFailure);
        Assert.Equal(InventoryErrors.InvalidId.Code, result.Error.Code);
    }

    [Fact]
    public void Create_WithNegativeQuantity_Fails()
    {
        var result = InventoryItem.Create(ProductId.New(), -1);

        Assert.True(result.IsFailure);
        Assert.Equal(InventoryErrors.InvalidQuantity.Code, result.Error.Code);
    }

    [Fact]
    public void Create_RaisesInventoryItemCreatedDomainEvent()
    {
        var productId = ProductId.New();

        var inventoryItem = InventoryItem.Create(productId, 10).Value;

        var domainEvent = Assert.Single(inventoryItem.GetDomainEvents());
        var created = Assert.IsType<InventoryItemCreatedDomainEvent>(domainEvent);
        Assert.Equal(productId, created.ProductId);
        Assert.Single(inventoryItem.Ledger, e => e.Type == StockLedgerEntryType.Initial);
    }

    [Fact]
    public void Adjust_WithPositiveDelta_IncreasesStockAndRaisesEvent()
    {
        var inventoryItem = Create(10);
        inventoryItem.ClearDomainEvents();

        var result = inventoryItem.Adjust(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(15, inventoryItem.AvailableQuantity);

        var adjusted = Assert.IsType<StockAdjustedDomainEvent>(Assert.Single(inventoryItem.GetDomainEvents()));
        Assert.Equal(5, adjusted.AdjustedDelta);
        Assert.Equal(15, adjusted.AvailableQuantity);
    }

    [Fact]
    public void Adjust_WritesSignedAdjustmentLedgerEntries()
    {
        var inventoryItem = Create(10);

        Assert.True(inventoryItem.Adjust(5).IsSuccess);
        Assert.True(inventoryItem.Adjust(-4).IsSuccess);

        var adjustments = inventoryItem.Ledger
            .Where(e => e.Type == StockLedgerEntryType.Adjustment)
            .ToList();

        Assert.Equal(2, adjustments.Count);
        Assert.Equal(5, adjustments[0].Quantity);
        Assert.Equal(-4, adjustments[1].Quantity);
        Assert.All(adjustments, e => Assert.Null(e.OrderId));
    }

    [Fact]
    public void Adjust_WithNegativeDelta_DecreasesStock()
    {
        var inventoryItem = Create(10);

        var result = inventoryItem.Adjust(-4);

        Assert.True(result.IsSuccess);
        Assert.Equal(6, inventoryItem.AvailableQuantity);
    }

    [Fact]
    public void Adjust_WithZeroDelta_Fails()
    {
        var inventoryItem = Create(10);

        var result = inventoryItem.Adjust(0);

        Assert.True(result.IsFailure);
        Assert.Equal(InventoryErrors.InvalidQuantity.Code, result.Error.Code);
        Assert.Equal(10, inventoryItem.AvailableQuantity);
    }

    [Fact]
    public void Adjust_BelowZero_Fails()
    {
        var inventoryItem = Create(10);

        var result = inventoryItem.Adjust(-11);

        Assert.True(result.IsFailure);
        Assert.Equal(InventoryErrors.InsufficientStock.Code, result.Error.Code);
        Assert.Equal(10, inventoryItem.AvailableQuantity);
    }

    [Fact]
    public void Adjust_ToExactlyZero_Succeeds()
    {
        var inventoryItem = Create(10);

        var result = inventoryItem.Adjust(-10);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, inventoryItem.AvailableQuantity);
    }

    [Fact]
    public void Reserve_WithSufficientStock_DecrementsAndWritesLedgerEntry()
    {
        var inventoryItem = Create(10);
        inventoryItem.ClearDomainEvents();
        var orderId = OrderId.New();

        var result = inventoryItem.Reserve(orderId, 3);

        Assert.True(result.IsSuccess);
        Assert.Equal(7, inventoryItem.AvailableQuantity);

        var entry = Assert.Single(inventoryItem.Ledger, e => e.Type == StockLedgerEntryType.Reserve);
        Assert.Equal(orderId, entry.OrderId);
        Assert.Equal(StockLedgerEntryType.Reserve, entry.Type);
        Assert.Equal(3, entry.Quantity);

        // Exactly one intent event per stock change — no generic state event.
        var events = inventoryItem.GetDomainEvents().ToList();
        var reserved = Assert.IsType<StockReservedDomainEvent>(Assert.Single(events));
        Assert.Equal(orderId, reserved.OrderId);
        Assert.Equal(3, reserved.ReservedQuantity);
        Assert.Equal(7, reserved.AvailableQuantity);
    }

    [Fact]
    public void Reserve_WhenReplayed_IsIdempotent()
    {
        var inventoryItem = Create(10);
        var orderId = OrderId.New();
        Assert.True(inventoryItem.Reserve(orderId, 4).IsSuccess);
        var afterFirstReserve = inventoryItem.AvailableQuantity;
        inventoryItem.ClearDomainEvents();

        // Same quantity and even a different one: the first reserve wins and a
        // replay neither deducts stock, appends entries, nor raises events.
        var result = inventoryItem.Reserve(orderId, 4);

        Assert.True(result.IsSuccess);
        Assert.Equal(afterFirstReserve, inventoryItem.AvailableQuantity);
        Assert.Equal(1, inventoryItem.Ledger.Count(e => e.Type == StockLedgerEntryType.Reserve));
        Assert.Empty(inventoryItem.GetDomainEvents());

        Assert.True(inventoryItem.Reserve(orderId, 9).IsSuccess);
        Assert.Equal(afterFirstReserve, inventoryItem.AvailableQuantity);
    }

    [Fact]
    public void Reserve_AfterReleaseForTheSameOrder_ReservesAgain()
    {
        // Outstanding-based idempotency: a released reservation is no longer
        // outstanding, so the same order may reserve again. This pins the
        // semantics against a simplification to "any reserve entry exists".
        var inventoryItem = Create(10);
        var orderId = OrderId.New();
        inventoryItem.ClearDomainEvents();
        Assert.True(inventoryItem.Reserve(orderId, 4).IsSuccess);
        Assert.True(inventoryItem.Release(orderId).IsSuccess);
        inventoryItem.ClearDomainEvents();

        var result = inventoryItem.Reserve(orderId, 3);

        Assert.True(result.IsSuccess);
        Assert.Equal(7, inventoryItem.AvailableQuantity);
        Assert.Equal(2, inventoryItem.Ledger.Count(e => e.Type == StockLedgerEntryType.Reserve));
        Assert.Single(inventoryItem.Ledger, e => e.Type == StockLedgerEntryType.Release);
        Assert.Equal(inventoryItem.AvailableQuantity, inventoryItem.ReconstructedAvailability());

        // The second reservation raises a fresh event of its own.
        var reservedAgain = Assert.IsType<StockReservedDomainEvent>(Assert.Single(inventoryItem.GetDomainEvents()));
        Assert.Equal(3, reservedAgain.ReservedQuantity);
        Assert.Equal(7, reservedAgain.AvailableQuantity);
    }

    [Fact]
    public void Reserve_ReplaySucceedsEvenWhenStockCoversNeitherQuantity()
    {
        // A replayed checkout must not turn into InsufficientStock just because
        // the original reserve already consumed the availability.
        var inventoryItem = Create(2);
        var orderId = OrderId.New();
        Assert.True(inventoryItem.Reserve(orderId, 2).IsSuccess);

        var result = inventoryItem.Reserve(orderId, 2);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, inventoryItem.AvailableQuantity);
        Assert.Equal(1, inventoryItem.Ledger.Count(e => e.Type == StockLedgerEntryType.Reserve));
    }

    [Fact]
    public void Reserve_WithInsufficientStock_Fails()
    {
        var inventoryItem = Create(2);

        var result = inventoryItem.Reserve(OrderId.New(), 3);

        Assert.True(result.IsFailure);
        Assert.Equal(InventoryErrors.InsufficientStock.Code, result.Error.Code);
        Assert.Equal(2, inventoryItem.AvailableQuantity);
        Assert.DoesNotContain(inventoryItem.Ledger, e => e.Type == StockLedgerEntryType.Reserve);
    }

    [Fact]
    public void Reserve_WithZeroOrNegativeQuantity_Fails()
    {
        var inventoryItem = Create(2);

        Assert.Equal(
            InventoryErrors.InvalidQuantity.Code,
            inventoryItem.Reserve(OrderId.New(), 0).Error.Code);
        Assert.Equal(
            InventoryErrors.InvalidQuantity.Code,
            inventoryItem.Reserve(OrderId.New(), -1).Error.Code);

        Assert.Equal(2, inventoryItem.AvailableQuantity);
        Assert.DoesNotContain(inventoryItem.Ledger, e => e.Type == StockLedgerEntryType.Reserve);
    }

    [Fact]
    public void Release_RestoresOutstandingReservationAndWritesReleaseEntry()
    {
        var inventoryItem = Create(10);
        var orderId = OrderId.New();
        Assert.True(inventoryItem.Reserve(orderId, 4).IsSuccess);
        inventoryItem.ClearDomainEvents();

        var result = inventoryItem.Release(orderId);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, inventoryItem.AvailableQuantity);

        var releaseEntry = inventoryItem.Ledger.Single(e => e.Type == StockLedgerEntryType.Release);
        Assert.Equal(orderId, releaseEntry.OrderId);
        Assert.Equal(4, releaseEntry.Quantity);

        // Exactly one intent event per stock change — no generic state event.
        var events = inventoryItem.GetDomainEvents().ToList();
        var released = Assert.IsType<StockReleasedDomainEvent>(Assert.Single(events));
        Assert.Equal(orderId, released.OrderId);
        Assert.Equal(4, released.ReleasedQuantity);
        Assert.Equal(10, released.AvailableQuantity);
    }

    [Fact]
    public void Release_WhenReplayed_IsIdempotent()
    {
        var inventoryItem = Create(10);
        var orderId = OrderId.New();
        Assert.True(inventoryItem.Reserve(orderId, 4).IsSuccess);

        Assert.True(inventoryItem.Release(orderId).IsSuccess);
        var afterFirstRelease = inventoryItem.AvailableQuantity;

        var result = inventoryItem.Release(orderId);

        Assert.True(result.IsSuccess);
        Assert.Equal(afterFirstRelease, inventoryItem.AvailableQuantity);
        Assert.Equal(1, inventoryItem.Ledger.Count(e => e.Type == StockLedgerEntryType.Release));
    }

    [Fact]
    public void Release_WithNoReservation_IsNoOp()
    {
        var inventoryItem = Create(10);

        var result = inventoryItem.Release(OrderId.New());

        Assert.True(result.IsSuccess);
        Assert.Equal(10, inventoryItem.AvailableQuantity);
        Assert.DoesNotContain(inventoryItem.Ledger, e => e.Type == StockLedgerEntryType.Release);
    }

    [Fact]
    public void Release_OnlyReleasesOutstandingEntriesForThatOrder()
    {
        var inventoryItem = Create(10);
        var firstOrder = OrderId.New();
        var secondOrder = OrderId.New();
        Assert.True(inventoryItem.Reserve(firstOrder, 4).IsSuccess);
        Assert.True(inventoryItem.Reserve(secondOrder, 2).IsSuccess);

        var result = inventoryItem.Release(firstOrder);

        Assert.True(result.IsSuccess);
        Assert.Equal(8, inventoryItem.AvailableQuantity);

        // Releasing again for the same order restores nothing further.
        Assert.True(inventoryItem.Release(firstOrder).IsSuccess);
        Assert.Equal(8, inventoryItem.AvailableQuantity);
        Assert.Equal(1, inventoryItem.Ledger.Count(e => e.Type == StockLedgerEntryType.Release));
    }

    [Fact]
    public void Ledger_SignedSum_ReconstructsAvailability()
    {
        var inventoryItem = Create(10);
        var orderId = OrderId.New();

        Assert.True(inventoryItem.Adjust(5).IsSuccess);
        Assert.True(inventoryItem.Reserve(orderId, 4).IsSuccess);
        Assert.True(inventoryItem.Adjust(-2).IsSuccess);
        Assert.True(inventoryItem.Release(orderId).IsSuccess);

        Assert.Equal(13, inventoryItem.AvailableQuantity);
        Assert.Equal(inventoryItem.AvailableQuantity, inventoryItem.ReconstructedAvailability());
    }

    private static InventoryItem Create(int quantity)
        => InventoryItem.Create(ProductId.New(), quantity).Value;
}
