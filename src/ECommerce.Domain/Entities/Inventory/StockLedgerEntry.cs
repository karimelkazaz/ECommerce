using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Orders;

namespace ECommerce.Domain.Entities.Inventory;

public class StockLedgerEntry : Entity<Guid>
{
    public OrderId OrderId { get; private set; }
    public StockLedgerEntryType Type { get; private set; }
    public int Quantity { get; private set; }

    private StockLedgerEntry() { }

    private StockLedgerEntry(OrderId orderId, StockLedgerEntryType type, int quantity)
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
}
