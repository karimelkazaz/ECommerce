using ECommerce.Application.Common.Interfaces;
using ECommerce.Infrastructure.Persistence.DbContexts;
using ECommerce.Infrastructure.Persistence.Identity;
using ECommerce.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
 
namespace ECommerce.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                      ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            services.AddScoped<AuditInterceptor>();
            services.AddScoped<ISoftDeleteInterceptor, SoftDeleteInterceptor>();

            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                var auditInterceptor = sp.GetRequiredService<AuditInterceptor>();

                options.UseSqlServer(connectionString, sql =>
                       sql.MigrationsHistoryTable("__ApplicationMigrationsHistory", "app"))
                    .EnableSensitiveDataLogging()
                    .AddInterceptors(auditInterceptor);
            });

            return services;
        }
    }
}