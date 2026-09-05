namespace ECommerce.Application.Auth;

public sealed record AuthResponse(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    IReadOnlyList<string> Roles,
    string Token,
    DateTimeOffset ExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);