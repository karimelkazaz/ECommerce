using ECommerce.Domain.Entities.Inventory;
using ECommerce.Domain.Entities.Inventory.Events;
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
        Assert.Equal(0, inventoryItem.Ledger.Count);
    }

    [Fact]
    public void Adjust_WithPositiveDelta_IncreasesStockAndRaisesEvent()
    {
        var inventoryItem = Create(10);
        inventoryItem.ClearDomainEvents();

        var result = inventoryItem.Adjust(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(15, inventoryItem.AvailableQuantity);
        Assert.IsType<StockChangedDomainEvent>(Assert.Single(inventoryItem.GetDomainEvents()));
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
        var orderId = Guid.NewGuid();

        var result = inventoryItem.Reserve(orderId, 3);

        Assert.True(result.IsSuccess);
        Assert.Equal(7, inventoryItem.AvailableQuantity);

        var entry = Assert.Single(inventoryItem.Ledger);
        Assert.Equal(orderId, entry.OrderId);
        Assert.Equal(StockLedgerEntryType.Reserve, entry.Type);
        Assert.Equal(3, entry.Quantity);
        Assert.IsType<StockChangedDomainEvent>(Assert.Single(inventoryItem.GetDomainEvents()));
    }

    [Fact]
    public void Reserve_WithInsufficientStock_Fails()
    {
        var inventoryItem = Create(2);

        var result = inventoryItem.Reserve(Guid.NewGuid(), 3);

        Assert.True(result.IsFailure);
        Assert.Equal(InventoryErrors.InsufficientStock.Code, result.Error.Code);
        Assert.Equal(2, inventoryItem.AvailableQuantity);
        Assert.Empty(inventoryItem.Ledger);
    }

    [Fact]
    public void Reserve_WithZeroOrNegativeQuantity_Fails()
    {
        var inventoryItem = Create(2);

        Assert.Equal(
            InventoryErrors.InvalidQuantity.Code,
            inventoryItem.Reserve(Guid.NewGuid(), 0).Error.Code);
        Assert.Equal(
            InventoryErrors.InvalidQuantity.Code,
            inventoryItem.Reserve(Guid.NewGuid(), -1).Error.Code);

        Assert.Equal(2, inventoryItem.AvailableQuantity);
        Assert.Empty(inventoryItem.Ledger);
    }

    [Fact]
    public void Release_RestoresOutstandingReservationAndWritesReleaseEntry()
    {
        var inventoryItem = Create(10);
        var orderId = Guid.NewGuid();
        Assert.True(inventoryItem.Reserve(orderId, 4).IsSuccess);
        inventoryItem.ClearDomainEvents();

        var result = inventoryItem.Release(orderId);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, inventoryItem.AvailableQuantity);

        var releaseEntry = inventoryItem.Ledger.Single(e => e.Type == StockLedgerEntryType.Release);
        Assert.Equal(orderId, releaseEntry.OrderId);
        Assert.Equal(4, releaseEntry.Quantity);
        Assert.IsType<StockChangedDomainEvent>(Assert.Single(inventoryItem.GetDomainEvents()));
    }

    [Fact]
    public void Release_WhenReplayed_IsIdempotent()
    {
        var inventoryItem = Create(10);
        var orderId = Guid.NewGuid();
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

        var result = inventoryItem.Release(Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal(10, inventoryItem.AvailableQuantity);
        Assert.Empty(inventoryItem.Ledger);
    }

    [Fact]
    public void Release_OnlyReleasesOutstandingEntriesForThatOrder()
    {
        var inventoryItem = Create(10);
        var firstOrder = Guid.NewGuid();
        var secondOrder = Guid.NewGuid();
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

    private static InventoryItem Create(int quantity)
        => InventoryItem.Create(ProductId.New(), quantity).Value;
}
