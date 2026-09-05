using ECommerce.Domain.Common;
using ECommerce.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Persistence.Seeding;

public sealed class IdentitySeeder(
    RoleManager<ApplicationRole> roleManager,
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    ILogger<IdentitySeeder> logger) : IDataSeeder
{
    public int Order => 0;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        foreach (var role in new[] { Roles.Admin, Roles.Customer })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole(role));
            }
        }

        var adminEmail = configuration["Identity:DefaultAdmin:Email"];
        var adminPassword = configuration["Identity:DefaultAdmin:Password"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogInformation("No default admin configured; skipping admin seeding.");
            return;
        }

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is not null)
        {
            return;
        }

        admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "Admin",
            LastName = "Administrator",
            EmailConfirmed = true,
        };

        var createResult = await userManager.CreateAsync(admin, adminPassword);
        if (createResult.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, Roles.Admin);
        }
    }
}