using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities.RefreshTokens;

public sealed class RefreshToken : Entity<RefreshTokenId>
{
    public const int MaxTokenHashLength = 64;

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = null!;

    public DateTimeOffset ExpiresOnUtc { get; private set; }

    public DateTimeOffset? RevokedOnUtc { get; private set; }

    public bool IsRevoked => RevokedOnUtc is not null;

    public bool IsExpired(DateTimeOffset asOfUtc) => ExpiresOnUtc <= asOfUtc;

    public bool IsActive(DateTimeOffset asOfUtc) => !IsRevoked && !IsExpired(asOfUtc);

    private RefreshToken()
    {
    }

    private RefreshToken(
        RefreshTokenId id,
        Guid userId,
        string tokenHash,
        DateTimeOffset createdOnUtc,
        DateTimeOffset expiresOnUtc)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        CreatedOnUtc = createdOnUtc;
        ExpiresOnUtc = expiresOnUtc;
    }

    public static Result<RefreshToken> Create(
        RefreshTokenId id,
        Guid userId,
        string tokenHash,
        DateTimeOffset createdOnUtc,
        DateTimeOffset expiresOnUtc)
    {
        if (id == default)
        {
            return Result.Failure<RefreshToken>(RefreshTokenErrors.InvalidId);
        }

        if (userId == Guid.Empty)
        {
            return Result.Failure<RefreshToken>(RefreshTokenErrors.InvalidUserId);
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            return Result.Failure<RefreshToken>(RefreshTokenErrors.InvalidTokenHash);
        }

        if (expiresOnUtc <= createdOnUtc)
        {
            return Result.Failure<RefreshToken>(RefreshTokenErrors.InvalidExpiration);
        }

        var refreshToken = new RefreshToken(id, userId, tokenHash, createdOnUtc, expiresOnUtc);
        return Result.Success(refreshToken);
    }

    public Result Revoke(DateTimeOffset revokedOnUtc)
    {
        if (IsRevoked)
        {
            return Result.Failure(RefreshTokenErrors.AlreadyRevoked);
        }

        RevokedOnUtc = revokedOnUtc;
        return Result.Success();
    }
}