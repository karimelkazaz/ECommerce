using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities.Inventory;

public class StockLedgerEntry : Entity<Guid>
{
    public Guid OrderId { get; private set; }
    public StockLedgerEntryType Type { get; private set; }
    public int Quantity { get; private set; }

    private StockLedgerEntry() { }

    private StockLedgerEntry(Guid orderId, StockLedgerEntryType type, int quantity)
        : base(Guid.NewGuid())
    {
        OrderId = orderId;
        Type = type;
        Quantity = quantity;
    }

    internal static StockLedgerEntry Reserve(Guid orderId, int quantity)
        => new(orderId, StockLedgerEntryType.Reserve, quantity);

    internal static StockLedgerEntry Release(Guid orderId, int quantity)
        => new(orderId, StockLedgerEntryType.Release, quantity);
}
