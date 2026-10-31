using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities.DeliveryMethods;
using ECommerce.Domain.Entities.Orders;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.Entities.ProductsBrand;
using ECommerce.Domain.Entities.ProductsType;
using ECommerce.Domain.Entities.UserAddresses;
using ECommerce.Infrastructure.Persistence.Configurations;
using ECommerce.Infrastructure.Persistence.Inbox;
using ECommerce.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.DbContexts;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Product> ProductsSet => Set<Product>();
    public DbSet<ProductBrand> BrandsSet => Set<ProductBrand>();
    public DbSet<ProductType> TypesSet => Set<ProductType>();
    public DbSet<DeliveryMethod> DeliveryMethodsSet => Set<DeliveryMethod>();
    public DbSet<Order> OrdersSet => Set<Order>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    IQueryable<Product> IApplicationDbContext.Products => ProductsSet.AsNoTracking();
    IQueryable<ProductBrand> IApplicationDbContext.Brands => BrandsSet.AsNoTracking();
    IQueryable<ProductType> IApplicationDbContext.Types => TypesSet.AsNoTracking();
    IQueryable<DeliveryMethod> IApplicationDbContext.DeliveryMethods => DeliveryMethodsSet.AsNoTracking();
    IQueryable<Order> IApplicationDbContext.Orders => OrdersSet.AsNoTracking();
    IQueryable<UserAddress> IApplicationDbContext.UserAddresses => UserAddresses.AsNoTracking();

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (Database.CurrentTransaction is not null)
        {
            throw new InvalidOperationException(
                "A transaction is already active. Consumers must not nest transactions.");
        }

        await using var transaction = await Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            try
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
            catch
            {
                // Preserve the original failure. Disposing the transaction will
                // roll it back if it is still active.
            }

            ChangeTracker.Clear();
            throw;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly,
            type => type.Namespace == "ECommerce.Infrastructure.Persistence.Configurations"
                 && type != typeof(RefreshTokenConfiguration)
                 && type != typeof(OtpCodeConfiguration));

        base.OnModelCreating(modelBuilder);
    }
}
