using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.Entities.ProductsBrand;
using ECommerce.Domain.Entities.ProductsType;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.DbContexts;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options), IApplicationDbContext
{
    public DbSet<Product> ProductsSet => Set<Product>();
    public DbSet<ProductBrand> BrandsSet => Set<ProductBrand>();
    public DbSet<ProductType> TypesSet => Set<ProductType>();

    IQueryable<Product> IApplicationDbContext.Products => ProductsSet.AsNoTracking();
    IQueryable<ProductBrand> IApplicationDbContext.Brands => BrandsSet.AsNoTracking();
    IQueryable<ProductType> IApplicationDbContext.Types => TypesSet.AsNoTracking();



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly,
            type => type.Namespace == "ECommerce.Infrastructure.Persistence.Configurations");

        base.OnModelCreating(modelBuilder);
    }
}