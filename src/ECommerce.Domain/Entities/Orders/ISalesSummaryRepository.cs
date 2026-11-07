namespace ECommerce.Domain.Entities.Orders;

public interface ISalesSummaryRepository
{
    Task<SalesSummary?> GetByDayAsync(DateOnly day, string currencyCode, CancellationToken ct = default);

    void Add(SalesSummary salesSummary);
}
