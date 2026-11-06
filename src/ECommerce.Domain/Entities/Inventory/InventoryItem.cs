using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Inventory.Events;
using ECommerce.Domain.Entities.Orders;
using ECommerce.Domain.Entities.Products;

namespace ECommerce.Domain.Entities.Inventory;

public sealed class InventoryItem : Entity<InventoryItemId>
{
    private readonly List<StockLedgerEntry> _ledger = [];

    public ProductId ProductId { get; private set; }

    public int AvailableQuantity { get; private set; }

    // EF-managed optimistic concurrency token (rowversion). Every stock mutation
    // saves through this aggregate, so a conflicting concurrent save fails loudly
    // instead of silently overwriting another worker's change.
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyList<StockLedgerEntry> Ledger => _ledger.AsReadOnly();

    private InventoryItem() { }

    private InventoryItem(InventoryItemId id, ProductId productId, int quantity)
        : base(id)
    {
        ProductId = productId;
        AvailableQuantity = quantity;
    }

    public static Result<InventoryItem> Create(ProductId productId, int quantity)
    {
        if (productId == default)
            return Result.Failure<InventoryItem>(InventoryErrors.InvalidId);

        if (quantity < 0)
            return Result.Failure<InventoryItem>(InventoryErrors.InvalidQuantity);

        var inventoryItem = new InventoryItem(InventoryItemId.New(), productId, quantity);

        // Baseline ledger entry: every unit of AvailableQuantity must be traceable
        // to the ledger, so a non-zero creation seeds the history (signed convention:
        // Initial increases availability).
        if (quantity > 0)
            inventoryItem._ledger.Add(StockLedgerEntry.Initial(quantity));

        inventoryItem.RaiseDomainEvent(new InventoryItemCreatedDomainEvent(inventoryItem.Id, productId));

        return Result.Success(inventoryItem);
    }

    public Result Adjust(int delta)
    {
        if (delta == 0)
            return Result.Failure(InventoryErrors.InvalidQuantity);

        if (AvailableQuantity + delta < 0)
            return Result.Failure(InventoryErrors.InsufficientStock);

        AvailableQuantity += delta;
        _ledger.Add(StockLedgerEntry.Adjustment(delta));
        RaiseDomainEvent(new StockAdjustedDomainEvent(Id, ProductId, delta, AvailableQuantity));

        return Result.Success();
    }

    public Result Reserve(OrderId orderId, int quantity)
    {
        if (quantity < 1)
            return Result.Failure(InventoryErrors.InvalidQuantity);

        // Idempotent: a replayed reserve for an order that still holds stock
        // resolves to success without deducting again — mirroring Release, the
        // first reservation for an order wins. Checked before availability, since
        // the reserve behind the replay has already reduced it.
        if (OutstandingReservationsFor(orderId) > 0)
            return Result.Success();

        if (AvailableQuantity < quantity)
            return Result.Failure(InventoryErrors.InsufficientStock);

        AvailableQuantity -= quantity;
        _ledger.Add(StockLedgerEntry.Reserve(orderId, quantity));
        RaiseDomainEvent(new StockReservedDomainEvent(Id, ProductId, orderId, quantity, AvailableQuantity));

        return Result.Success();
    }

    public Result Release(OrderId orderId)
    {
        // Idempotent: releases replayed past the ledger resolve to success without
        // touching stock, so a cancelled order can never be restocked twice.
        var outstanding = OutstandingReservationsFor(orderId);

        if (outstanding <= 0)
            return Result.Success();

        AvailableQuantity += outstanding;
        _ledger.Add(StockLedgerEntry.Release(orderId, outstanding));
        RaiseDomainEvent(new StockReleasedDomainEvent(Id, ProductId, orderId, outstanding, AvailableQuantity));

        return Result.Success();
    }

    private int OutstandingReservationsFor(OrderId orderId)
        => _ledger.Where(e => e.OrderId == orderId && e.Type == StockLedgerEntryType.Reserve)
                .Sum(e => e.Quantity)
            - _ledger.Where(e => e.OrderId == orderId && e.Type == StockLedgerEntryType.Release)
                .Sum(e => e.Quantity);

    /// <summary>
    /// Reconstructs the availability implied by the ledger. Reserve decreases (- Qty),
    /// Initial/Release increase (+ Qty), Adjustment carries its signed delta.
    /// The aggregate invariant is: ReconstructedAvailability == AvailableQuantity.
    /// </summary>
    public int ReconstructedAvailability()
        => _ledger.Sum(e => e.Type switch
        {
            StockLedgerEntryType.Reserve => -e.Quantity,
            StockLedgerEntryType.Adjustment => e.Quantity, // signed
            _ => e.Quantity, // Initial, Release
        });
}
