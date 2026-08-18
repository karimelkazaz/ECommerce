using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.Entities.ProductsBrand;
using ECommerce.Domain.Entities.ProductsType;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.DbContexts;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options), IApplicationDbContext
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductBrand> Brands => Set<ProductBrand>();
    public DbSet<ProductType> Types => Set<ProductType>();

    IQueryable<Product> IApplicationDbContext.Products => Products.AsNoTracking();
    IQueryable<ProductBrand> IApplicationDbContext.Brands => Brands.AsNoTracking();
    IQueryable<ProductType> IApplicationDbContext.Types => Types.AsNoTracking();



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly,
            type => type.Namespace == "ECommerce.Infrastructure.Persistence.Configurations");

        base.OnModelCreating(modelBuilder);
    }
}