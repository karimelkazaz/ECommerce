namespace ECommerce.Application.Common.Interfaces;

/// <summary>
/// Threshold policy for stock-consumer follow-ups. Implemented against
/// configuration in Infrastructure; a disabled policy suppresses every
/// low-stock follow-up (total depletion is governed separately and always on).
/// </summary>
public interface IStockAttentionPolicy
{
    bool LowStockAlertsEnabled { get; }

    /// <summary>Inclusive availability threshold; ≤ this reads as "low stock".</summary>
    int LowStockThreshold { get; }
}
