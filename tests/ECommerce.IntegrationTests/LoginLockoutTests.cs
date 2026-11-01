using ECommerce.Domain.Common;
using ECommerce.Infrastructure.Persistence.DbContexts;
using ECommerce.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ECommerce.IntegrationTests;

public sealed class LoginLockoutTests
{
    [Fact]
    public async Task LoginAsync_EnablesLockoutOnPasswordFailure()
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "customer@example.com",
            UserName = "customer@example.com",
            EmailConfirmed = true,
        };
        var userManager = new StubUserManager(user);
        var signInManager = new CapturingSignInManager(userManager);
        var options = new DbContextOptionsBuilder<AppIdentityDbContext>().Options;
        var identityService = new IdentityService(
            userManager,
            signInManager,
            tokenService: null!,
            new AppIdentityDbContext(options),
            Microsoft.Extensions.Options.Options.Create(new JwtOptions()),
            NullLogger<IdentityService>.Instance);

        var result = await identityService.LoginAsync(user.Email, "wrong-password", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthErrors.InvalidCredentials.Code, result.Error.Code);
        Assert.True(signInManager.LockoutOnFailure);
    }

    private sealed class StubUserManager(ApplicationUser user)
        : UserManager<ApplicationUser>(
            store: new StubUserStore(),
            Microsoft.Extensions.Options.Options.Create(new IdentityOptions()),
            passwordHasher: null!,
            userValidators: [],
            passwordValidators: [],
            keyNormalizer: null!,
            errors: null!,
            services: null!,
            NullLogger<UserManager<ApplicationUser>>.Instance)
    {
        public override Task<ApplicationUser?> FindByEmailAsync(string email)
            => Task.FromResult<ApplicationUser?>(user);
    }

    private sealed class StubUserStore : IUserStore<ApplicationUser>
    {
        public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken)
            => Task.FromResult(user.Id.ToString());

        public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
            => Task.FromResult(user.UserName);

        public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
            => Task.FromResult(user.NormalizedUserName);

        public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public void Dispose()
        {
        }
    }

    private sealed class CapturingSignInManager(UserManager<ApplicationUser> userManager)
        : SignInManager<ApplicationUser>(
            userManager,
            contextAccessor: new HttpContextAccessor(),
            claimsFactory: new UserClaimsPrincipalFactory<ApplicationUser>(
                userManager,
                Microsoft.Extensions.Options.Options.Create(new IdentityOptions())),
            Microsoft.Extensions.Options.Options.Create(new IdentityOptions()),
            NullLogger<SignInManager<ApplicationUser>>.Instance,
            schemes: null!,
            confirmation: null!)
    {
        public bool LockoutOnFailure { get; private set; }

        public override Task<SignInResult> CheckPasswordSignInAsync(
            ApplicationUser user,
            string password,
            bool lockoutOnFailure)
        {
            LockoutOnFailure = lockoutOnFailure;
            return Task.FromResult(SignInResult.Failed);
        }
    }
}