using ECommerce.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Persistence.Inventory;

public sealed class StockAttentionPolicy(IOptions<StockAlertsOptions> options) : IStockAttentionPolicy
{
    public bool LowStockAlertsEnabled => options.Value.LowStockAlertsEnabled;

    public int LowStockThreshold => options.Value.LowStockThreshold;
}
