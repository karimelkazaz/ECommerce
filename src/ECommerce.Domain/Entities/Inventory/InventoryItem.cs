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
        RaiseDomainEvent(new StockChangedDomainEvent(Id, ProductId, AvailableQuantity));

        return Result.Success();
    }

    public Result Reserve(OrderId orderId, int quantity)
    {
        if (quantity < 1)
            return Result.Failure(InventoryErrors.InvalidQuantity);

        if (AvailableQuantity < quantity)
            return Result.Failure(InventoryErrors.InsufficientStock);

        AvailableQuantity -= quantity;
        _ledger.Add(StockLedgerEntry.Reserve(orderId, quantity));
        RaiseDomainEvent(new StockChangedDomainEvent(Id, ProductId, AvailableQuantity));

        return Result.Success();
    }

    public Result Release(OrderId orderId)
    {
        // Idempotent: releases replayed past the ledger resolve to success without
        // touching stock, so a cancelled order can never be restocked twice.
        var outstanding =
            _ledger.Where(e => e.OrderId == orderId && e.Type == StockLedgerEntryType.Reserve)
                .Sum(e => e.Quantity)
            - _ledger.Where(e => e.OrderId == orderId && e.Type == StockLedgerEntryType.Release)
                .Sum(e => e.Quantity);

        if (outstanding <= 0)
            return Result.Success();

        AvailableQuantity += outstanding;
        _ledger.Add(StockLedgerEntry.Release(orderId, outstanding));
        RaiseDomainEvent(new StockChangedDomainEvent(Id, ProductId, AvailableQuantity));

        return Result.Success();
    }
}
