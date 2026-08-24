using System.Security.Cryptography;
using GetCode.Application.Identity;

namespace GetCode.Infrastructure.Identity;

/// <summary>
/// M02-002 session token material: 256 bits of CSPRNG entropy encoded
/// url-safe as a cookie value; only the SHA-256 digest is ever persisted,
/// so a database leak does not yield usable session tokens.
/// </summary>
public sealed class CryptographicSessionTokens : ISessionTokenProvider
{
    public string CreateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}
