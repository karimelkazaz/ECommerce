using System.Security.Cryptography;

namespace ECommerce.Infrastructure.Persistence.Identity;

public static class RefreshTokenFactory
{
    public static string Generate()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)).TrimEnd('=');

    public static string Hash(string token)
        => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
}