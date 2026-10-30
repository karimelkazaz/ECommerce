using System.Text;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Options;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.DeliveryMethods;
using ECommerce.Domain.Entities.Orders;
using ECommerce.Domain.Entities.Products;
using ECommerce.Domain.Entities.ProductsBrand;
using ECommerce.Domain.Entities.ProductsType;
using ECommerce.Domain.Repositories;
using ECommerce.Infrastructure.Caching;
using ECommerce.Infrastructure.Email;
using ECommerce.Infrastructure.Persistence.DbContexts;
using ECommerce.Infrastructure.Persistence.Identity;
using ECommerce.Infrastructure.Persistence.Inbox;
using ECommerce.Infrastructure.Persistence.Interceptors;
using ECommerce.Infrastructure.Persistence.Otp;
using ECommerce.Infrastructure.Persistence.Outbox;
using ECommerce.Infrastructure.Persistence.Repositories;
using ECommerce.Infrastructure.Payment;
using ECommerce.Infrastructure.Persistence.Seeding;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

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

            services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));
            services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
            services.Configure<OtpOptions>(configuration.GetSection(OtpOptions.SectionName));

            services
                .AddIdentityCore<ApplicationUser>(options =>
                {
                    options.Password.RequiredLength = 8;
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = false;
                    options.User.RequireUniqueEmail = true;
                    options.SignIn.RequireConfirmedEmail = true;
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                })
                .AddRoles<ApplicationRole>()
                .AddEntityFrameworkStores<AppIdentityDbContext>()
                .AddDefaultTokenProviders()
                .AddSignInManager();

            var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                ?? throw new InvalidOperationException("JWT settings are missing. Configure the 'Jwt' section.");

            if (string.IsNullOrWhiteSpace(jwtOptions.SecretKey) || jwtOptions.SecretKey.Length < 32)
            {
                throw new InvalidOperationException(
                    "JWT settings are invalid. 'Jwt:SecretKey' must be at least 32 characters long.");
            }

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtOptions.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwtOptions.Audience,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey =
                            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                        NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
                        RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                        ClockSkew = TimeSpan.FromMinutes(1),
                    };
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy(
                    Roles.Admin,
                    policy => policy.RequireRole(Roles.Admin));
            });

            services.AddScoped<ITokenService, JwtTokenService>();
            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<IOtpService, OtpService>();

            var smtpHost = configuration["Smtp:Host"];
            if (!string.IsNullOrWhiteSpace(smtpHost))
            {
                services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
                services.AddScoped<IEmailSender, SmtpEmailSender>();
            }
            else
            {
                services.AddScoped<IEmailSender, LoggingEmailSender>();
            }

            services.AddScoped<OutboxInterceptor>();
            services.AddScoped<SoftDeleteInterceptor>();
            services.AddScoped<AuditInterceptor>();

            services.AddDbContext<AppIdentityDbContext>((sp, options) =>
            {
                var softDeleteInterceptor = sp.GetRequiredService<SoftDeleteInterceptor>();
                var auditInterceptor = sp.GetRequiredService<AuditInterceptor>();

                options.UseSqlServer(connectionString, sql =>
                       sql.MigrationsHistoryTable("__IdentityMigrationsHistory", "identity"))
                    .AddInterceptors(softDeleteInterceptor, auditInterceptor);

                if (enableSensitiveDataLogging)
                {
                    options.EnableSensitiveDataLogging();
                }
            });

            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                var outboxInterceptor = sp.GetRequiredService<OutboxInterceptor>();
                var softDeleteInterceptor = sp.GetRequiredService<SoftDeleteInterceptor>();
                var auditInterceptor = sp.GetRequiredService<AuditInterceptor>();

                options.UseSqlServer(connectionString, sql =>
                       sql.MigrationsHistoryTable("__ApplicationMigrationsHistory", "app"))
                    .AddInterceptors(outboxInterceptor, softDeleteInterceptor, auditInterceptor);

                if (enableSensitiveDataLogging)
                {
                    options.EnableSensitiveDataLogging();
                }
            });

            services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());
            services.AddScoped<IInbox, Inbox>();

            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IProductBrandRepository, ProductBrandRepository>();
            services.AddScoped<IProductTypeRepository, ProductTypeRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IDeliveryMethodRepository, DeliveryMethodRepository>();

            services.Configure<StripeOptions>(configuration.GetSection(StripeOptions.SectionName));
            services.AddScoped<IPaymentService, StripePaymentService>();

            services.AddScoped<IDataSeeder, ProductBrandSeeder>();
            services.AddScoped<IDataSeeder, ProductTypeSeeder>();
            services.AddScoped<IDataSeeder, DeliveryMethodSeeder>();
            services.AddScoped<IDataSeeder, IdentitySeeder>();
            services.AddScoped<DatabaseSeeder>();

            services.AddHostedService<OutboxProcessor>();
            AddBasketCaching(services, configuration);

            return services;
        }

        private static void AddBasketCaching(IServiceCollection services, IConfiguration configuration)
        {
            services.
                AddOptions<CacheEntryPolicy>("Basket")
                .Bind(configuration.GetSection("CachedAggregate:Basket"))
                .ValidateOnStart();

            services.AddSingleton<IValidateOptions<CacheEntryPolicy>, CacheEntryPolicyValidator>();

            var redisConnection = configuration.GetConnectionString("Redis");
            if (!string.IsNullOrWhiteSpace(redisConnection))
            {
                services.AddStackExchangeRedisCache(options =>
                    options.Configuration = redisConnection);
            }


            services.AddHybridCache();

            services.AddScoped(typeof(ICacheAggregateStore<>), typeof(CacheAggregateStore<>));
            services.AddScoped<IBasketStore, BasketStore>();
        }
    }
}
