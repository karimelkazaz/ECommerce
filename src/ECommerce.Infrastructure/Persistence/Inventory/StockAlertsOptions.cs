namespace ECommerce.Infrastructure.Persistence.Inventory;

public sealed class StockAlertsOptions
{
    public const string SectionName = "StockAlerts";

    /// <summary>
    /// Master switch for low-stock follow-up messages. Total-depletion
    /// notifications are emitted regardless of this setting.
    /// </summary>
    public bool LowStockAlertsEnabled { get; set; }

    /// <summary>
    /// Inclusive availability threshold below which a product counts as low
    /// stock; must be at least 1 when alerts are enabled.
    /// </summary>
    public int LowStockThreshold { get; set; } = 5;
}
