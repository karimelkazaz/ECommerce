using ECommerce.Domain.Entities.RefreshTokens;
using Xunit;

namespace ECommerce.Domain.UnitTests;

public class RefreshTokenTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static RefreshToken CreateActiveToken(
        DateTimeOffset now,
        int expirationDays = 30)
        => RefreshToken.Create(
                RefreshTokenId.New(),
                UserId,
                string.Concat(Enumerable.Repeat('A', 64)),
                now,
                now.AddDays(expirationDays))
            .Value;

    [Fact]
    public void Create_WithValidData_ReturnsSuccess()
    {
        var now = DateTimeOffset.UtcNow;

        var result = RefreshToken.Create(
            RefreshTokenId.New(),
            UserId,
            new string('A', 64),
            now,
            now.AddDays(30));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(result.Value.IsRevoked);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void Create_WithInvalidInput_ReturnsFailure(bool emptyId, bool emptyUser, bool emptyHash)
    {
        var now = DateTimeOffset.UtcNow;

        var result = RefreshToken.Create(
            emptyId ? default : RefreshTokenId.New(),
            emptyUser ? Guid.Empty : UserId,
            emptyHash ? string.Empty : new string('A', 64),
            now,
            now.AddDays(30));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Create_WithExpirationBeforeCreation_ReturnsFailure()
    {
        var now = DateTimeOffset.UtcNow;

        var result = RefreshToken.Create(
            RefreshTokenId.New(),
            UserId,
            new string('A', 64),
            now,
            now.AddMinutes(-1));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void IsActive_BeforeExpiry_ReturnsTrue()
    {
        var now = DateTimeOffset.UtcNow;
        var token = CreateActiveToken(now);

        Assert.True(token.IsActive(now.AddDays(10)));
    }

    [Fact]
    public void IsActive_AfterExpiry_ReturnsFalse()
    {
        var now = DateTimeOffset.UtcNow;
        var token = CreateActiveToken(now);

        Assert.True(token.IsExpired(now.AddDays(31)));
        Assert.False(token.IsActive(now.AddDays(31)));
    }

    [Fact]
    public void Revoke_SetsRevokedOnUtcAndDeactivatesToken()
    {
        var now = DateTimeOffset.UtcNow;
        var token = CreateActiveToken(now);

        var revokeResult = token.Revoke(now);

        Assert.True(revokeResult.IsSuccess);
        Assert.True(token.IsRevoked);
        Assert.Equal(now, token.RevokedOnUtc);
        Assert.False(token.IsActive(now.AddDays(1)));
    }

    [Fact]
    public void Revoke_Twice_ReturnsFailure()
    {
        var now = DateTimeOffset.UtcNow;
        var token = CreateActiveToken(now);

        token.Revoke(now);
        var secondRevoke = token.Revoke(now);

        Assert.True(secondRevoke.IsFailure);
        Assert.Equal(RefreshTokenErrors.AlreadyRevoked, secondRevoke.Error);
    }
}