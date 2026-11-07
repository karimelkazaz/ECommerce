using ECommerce.Domain.Entities.Orders;
using ECommerce.Infrastructure.Persistence.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public sealed class SalesSummaryRepository(ApplicationDbContext dbContext) : ISalesSummaryRepository
{
    public Task<SalesSummary?> GetByDayAsync(DateOnly day, string currencyCode, CancellationToken ct = default)
        => dbContext.SalesSummariesSet
            .FirstOrDefaultAsync(s => s.Day == day && s.CurrencyCode == currencyCode, ct);

    public void Add(SalesSummary salesSummary)
        => dbContext.SalesSummariesSet.Add(salesSummary);
}
