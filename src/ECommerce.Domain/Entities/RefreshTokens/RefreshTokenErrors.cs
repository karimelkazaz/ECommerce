using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities.RefreshTokens;

public static class RefreshTokenErrors
{
    public static readonly Error InvalidId = Error.Validation("RefreshToken.InvalidId", "Invalid refresh token ID.");
    public static readonly Error InvalidUserId = Error.Validation("RefreshToken.InvalidUserId", "User ID is required.");
    public static readonly Error InvalidTokenHash = Error.Validation("RefreshToken.InvalidTokenHash", "Token hash is required.");
    public static readonly Error InvalidExpiration = Error.Validation("RefreshToken.InvalidExpiration", "Expiration must be after the creation time.");
    public static readonly Error AlreadyRevoked = Error.Conflict("RefreshToken.AlreadyRevoked", "Refresh token is already revoked.");
}