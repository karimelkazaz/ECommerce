using ECommerce.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ECommerce.Infrastructure.Persistence.Interceptors;

public sealed class SoftDeleteInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void Apply(DbContext? dbContext)
    {
        if (dbContext is null) return;

        foreach (var entry in dbContext.ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State != EntityState.Deleted)
            {
                continue;
            }

            entry.State = EntityState.Modified;
            entry.Entity.MarkAsDeleted();

            // Owned types don't follow the owner's Deleted -> Modified transition,
            // so restore them explicitly or their columns get nulled out.
            foreach (var reference in entry.References.Where(r => r.Metadata.TargetEntityType.IsOwned()))
            {
                if (reference.TargetEntry is { State: EntityState.Deleted })
                {
                    reference.TargetEntry.State = EntityState.Modified;
                }
            }
        }
    }
}
