using ECommerce.Domain.Entities.RefreshTokens;
using ECommerce.Infrastructure.Persistence.Configurations;
using ECommerce.Infrastructure.Persistence.Identity;
using ECommerce.Infrastructure.Persistence.Otp;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.DbContexts;

public class AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(builder =>
        {
            builder.ToTable("Users", "identity");
            builder.Property(u => u.FirstName).HasMaxLength(100);
            builder.Property(u => u.LastName).HasMaxLength(100);
        });

        modelBuilder.Entity<ApplicationRole>(builder => builder.ToTable("Roles", "identity"));
        modelBuilder.Entity<IdentityUserClaim<Guid>>(builder => builder.ToTable("UserClaims", "identity"));
        modelBuilder.Entity<IdentityUserRole<Guid>>(builder => builder.ToTable("UserRoles", "identity"));
        modelBuilder.Entity<IdentityUserLogin<Guid>>(builder => builder.ToTable("UserLogins", "identity"));
        modelBuilder.Entity<IdentityRoleClaim<Guid>>(builder => builder.ToTable("RoleClaims", "identity"));
        modelBuilder.Entity<IdentityUserToken<Guid>>(builder => builder.ToTable("UserTokens", "identity"));

        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        modelBuilder.ApplyConfiguration(new OtpCodeConfiguration());
    }
}

