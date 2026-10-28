using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities.DeliveryMethods;
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
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    IQueryable<Product> IApplicationDbContext.Products => ProductsSet.AsNoTracking();
    IQueryable<ProductBrand> IApplicationDbContext.Brands => BrandsSet.AsNoTracking();
    IQueryable<ProductType> IApplicationDbContext.Types => TypesSet.AsNoTracking();
    IQueryable<DeliveryMethod> IApplicationDbContext.DeliveryMethods => DeliveryMethodsSet.AsNoTracking();
    IQueryable<UserAddress> IApplicationDbContext.UserAddresses => UserAddresses.AsNoTracking();

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
