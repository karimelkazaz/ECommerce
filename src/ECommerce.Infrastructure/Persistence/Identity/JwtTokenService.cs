using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ECommerce.Infrastructure.Persistence.Identity;

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAtUtc);

public interface ITokenService
{
    AccessToken CreateAccessToken(ApplicationUser user, IReadOnlyList<string> roles);
}

public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    public AccessToken CreateAccessToken(ApplicationUser user, IReadOnlyList<string> roles)
    {
        var jwt = options.Value;
        var now = DateTimeOffset.UtcNow;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey));
        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: now.AddMinutes(jwt.ExpirationMinutes).UtcDateTime,
            signingCredentials: signingCredentials);

        return new AccessToken(
            new JwtSecurityTokenHandler().WriteToken(token),
            now.AddMinutes(jwt.ExpirationMinutes));
    }
}