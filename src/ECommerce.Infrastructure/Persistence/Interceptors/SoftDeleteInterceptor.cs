using ECommerce.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Interceptors;

public sealed class SoftDeleteInterceptor : ISoftDeleteInterceptor
{
    public void Apply(DbContext dbContext)
    {
        foreach (var entry in dbContext.ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State != EntityState.Deleted)
            {
                continue;
            }

            entry.State = EntityState.Modified;
            entry.Entity.MarkAsDeleted();
        }
    }
}