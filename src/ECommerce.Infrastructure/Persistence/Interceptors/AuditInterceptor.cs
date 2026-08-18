using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ECommerce.Infrastructure.Persistence.Interceptors;

public sealed class UpdateAuditableEntitiesInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private readonly ICurrentUserService _currentUserService;

    // Inject the service here
    public UpdateAuditableEntitiesInterceptor(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }
    private void UpdateEntities(DbContext? dbContext)
    {
        if (dbContext is null) return;

        var utcNow = DateTimeOffset.UtcNow;
        var userId = _currentUserService.UserId ?? Guid.Empty;

        foreach (var entry in dbContext.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.SetCreated(utcNow, userId);
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.SetUpdated(utcNow, userId);
            }
        }
    }
}