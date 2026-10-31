using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Locking;

/// <summary>
/// Session-level SQL Server application locks. The lock lives on the connection
/// of the supplied context, so callers must keep that connection open until the
/// matching <see cref="ReleaseAsync"/> call.
/// </summary>
internal static class SqlApplicationLock
{
    public static async Task<bool> TryAcquireAsync(
        DbContext dbContext,
        string resource,
        CancellationToken cancellationToken)
    {
        var results = await dbContext.Database.SqlQuery<int>($@"
        DECLARE @result int;
        EXEC @result = sp_getapplock
        @Resource = {resource},
        @LockMode = 'Exclusive',
        @LockOwner = 'Session',
        @LockTimeout = 0;
        SELECT @result AS Value;")
            .ToListAsync(cancellationToken);

        return results.Single() >= 0;
    }

    public static Task ReleaseAsync(DbContext dbContext, string resource)
        => dbContext.Database.ExecuteSqlAsync(
            $"EXEC sp_releaseapplock @Resource = {resource}, @LockOwner = 'Session';");
}
