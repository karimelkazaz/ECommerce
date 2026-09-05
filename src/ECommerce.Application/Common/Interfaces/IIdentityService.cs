using ECommerce.Application.Auth;
using ECommerce.Domain.Common;

namespace ECommerce.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<bool> UserExistsAsync(string email, CancellationToken ct);

    Task<bool> IsEmailConfirmedAsync(string email, CancellationToken ct);

    Task<Result> CreateUserAsync(
        string firstName,
        string lastName,
        string email,
        string password,
        CancellationToken ct);

    Task<Result<AuthResponse>> LoginAsync(string email, string password, CancellationToken ct);

    Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken, CancellationToken ct);

    Task<Result> RevokeRefreshTokenAsync(string refreshToken, CancellationToken ct);
}