using ECommerce.Application.Auth;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities.RefreshTokens;
using ECommerce.Infrastructure.Persistence.DbContexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Persistence.Identity;

public sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ITokenService tokenService,
    AppIdentityDbContext dbContext,
    IOptions<JwtOptions> jwtOptions) : IIdentityService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<bool> UserExistsAsync(string email, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return await userManager.FindByEmailAsync(email) is not null;
    }

    public async Task<bool> IsEmailConfirmedAsync(string email, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var user = await userManager.FindByEmailAsync(email);
        return user is not null && await userManager.IsEmailConfirmedAsync(user);
    }

    public async Task<Result> CreateUserAsync(
        string firstName,
        string lastName,
        string email,
        string password,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = false,
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            if (createResult.Errors.Any(e => e.Code is "DuplicateUserName" or "DuplicateEmail"))
            {
                return Result.Failure(AuthErrors.EmailAlreadyRegistered);
            }

            return Result.Failure(AuthErrors.RegistrationFailed);
        }

        var roleResult = await userManager.AddToRoleAsync(user, Roles.Customer);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return Result.Failure(AuthErrors.RoleAssignmentFailed);
        }

        return Result.Success();
    }

    public async Task<Result<AuthResponse>> LoginAsync(string email, string password, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return Result.Failure<AuthResponse>(AuthErrors.InvalidCredentials);
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);

        if (signInResult.IsLockedOut)
        {
            return Result.Failure<AuthResponse>(AuthErrors.AccountLocked);
        }

        if (signInResult.IsNotAllowed)
        {
            return Result.Failure<AuthResponse>(AuthErrors.EmailNotConfirmed);
        }

        if (!signInResult.Succeeded)
        {
            return Result.Failure<AuthResponse>(AuthErrors.InvalidCredentials);
        }

        return await CreateAuthResponseAsync(user, ct);
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result.Failure<AuthResponse>(AuthErrors.InvalidRefreshToken);
        }

        var tokenHash = RefreshTokenFactory.Hash(refreshToken);
        var stored = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);

        if (stored is null || stored.IsRevoked)
        {
            return Result.Failure<AuthResponse>(AuthErrors.InvalidRefreshToken);
        }

        var now = DateTimeOffset.UtcNow;
        if (stored.IsExpired(now))
        {
            stored.Revoke(now);
            await dbContext.SaveChangesAsync(ct);
            return Result.Failure<AuthResponse>(AuthErrors.InvalidRefreshToken);
        }

        var user = await userManager.FindByIdAsync(stored.UserId.ToString());
        if (user is null)
        {
            stored.Revoke(now);
            await dbContext.SaveChangesAsync(ct);
            return Result.Failure<AuthResponse>(AuthErrors.InvalidRefreshToken);
        }

        stored.Revoke(now);

        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokenService.CreateAccessToken(user, roles.ToArray());
        var issued = await IssueRefreshTokenAsync(user.Id, ct);

        return new AuthResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email ?? string.Empty,
            roles.ToArray(),
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            issued.Token,
            issued.ExpiresOnUtc);
    }

    public async Task<Result> RevokeRefreshTokenAsync(string refreshToken, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result.Success();
        }

        var tokenHash = RefreshTokenFactory.Hash(refreshToken);
        var stored = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);

        if (stored is not null && !stored.IsRevoked)
        {
            stored.Revoke(DateTimeOffset.UtcNow);
            await dbContext.SaveChangesAsync(ct);
        }

        return Result.Success();
    }

    private async Task<Result<AuthResponse>> CreateAuthResponseAsync(ApplicationUser user, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var roles = await userManager.GetRolesAsync(user);
        var accessToken = tokenService.CreateAccessToken(user, roles.ToArray());
        var refreshToken = await IssueRefreshTokenAsync(user.Id, ct);

        return new AuthResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email ?? string.Empty,
            roles.ToArray(),
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            refreshToken.Token,
            refreshToken.ExpiresOnUtc);
    }

    private async Task<IssuedRefreshToken> IssueRefreshTokenAsync(Guid userId, CancellationToken ct)
    {
        var token = RefreshTokenFactory.Generate();
        var now = DateTimeOffset.UtcNow;

        var refreshToken = RefreshToken.Create(
                RefreshTokenId.New(),
                userId,
                RefreshTokenFactory.Hash(token),
                now,
                now.AddDays(_jwt.RefreshTokenExpirationDays))
            .Value;

        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync(ct);

        return new IssuedRefreshToken(token, refreshToken.ExpiresOnUtc);
    }
}

internal sealed record IssuedRefreshToken(string Token, DateTimeOffset ExpiresOnUtc);