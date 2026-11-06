using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Orders;

namespace ECommerce.Domain.Entities.Inventory;

public sealed class StockLedgerEntry : Entity<Guid>
{
    // Signed quantity convention: Reserve decreases availability (- Qty),
    // Initial and Release increase it (+ Qty), Adjustment stores its signed delta.
    public OrderId? OrderId { get; private set; }
    public StockLedgerEntryType Type { get; private set; }
    public int Quantity { get; private set; }

    private StockLedgerEntry() { }

    private StockLedgerEntry(OrderId? orderId, StockLedgerEntryType type, int quantity)
        : base(Guid.NewGuid())
    {
        OrderId = orderId;
        Type = type;
        Quantity = quantity;
    }

    internal static StockLedgerEntry Reserve(OrderId orderId, int quantity)
        => new(orderId, StockLedgerEntryType.Reserve, quantity);

    internal static StockLedgerEntry Release(OrderId orderId, int quantity)
        => new(orderId, StockLedgerEntryType.Release, quantity);

    internal static StockLedgerEntry Initial(int quantity)
        => new(null, StockLedgerEntryType.Initial, quantity);

    internal static StockLedgerEntry Adjustment(int delta)
        => new(null, StockLedgerEntryType.Adjustment, delta);
}
