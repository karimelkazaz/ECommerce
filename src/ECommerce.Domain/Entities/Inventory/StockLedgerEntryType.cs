using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities.Inventory;

public enum StockLedgerEntryType
{
    Reserve = 1,
    Release = 2,

    /// <summary>Baseline entry written when an inventory item is created with stock.</summary>
    Initial = 3,

    /// <summary>Manual correction; Quantity stores the signed delta.</summary>
    Adjustment = 4,
}
