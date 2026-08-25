using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.Entities.ProductsBrand;
using ECommerce.Domain.Entities.ProductsType;
using ECommerce.Infrastructure.Persistence.DbContexts;
using ECommerce.Infrastructure.Persistence.Identity;
using ECommerce.Infrastructure.Persistence.Interceptors;
using ECommerce.Infrastructure.Persistence.Repositories;
using ECommerce.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration,
            bool enableSensitiveDataLogging = false)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                      ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            services.AddScoped<SoftDeleteInterceptor>();
            services.AddScoped<AuditInterceptor>();

            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                var softDeleteInterceptor = sp.GetRequiredService<SoftDeleteInterceptor>();
                var auditInterceptor = sp.GetRequiredService<AuditInterceptor>();

                options.UseSqlServer(connectionString, sql =>
                       sql.MigrationsHistoryTable("__ApplicationMigrationsHistory", "app"))
                    .AddInterceptors(softDeleteInterceptor, auditInterceptor);

                if (enableSensitiveDataLogging)
                {
                    options.EnableSensitiveDataLogging();
                }
            });

            services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IProductBrandRepository, ProductBrandRepository>();
            services.AddScoped<IProductTypeRepository, ProductTypeRepository>();

            services.AddScoped<IDataSeeder, ProductBrandSeeder>();
            services.AddScoped<IDataSeeder, ProductTypeSeeder>();
            services.AddScoped<DatabaseSeeder>();


            return services;
        }
    }
}
