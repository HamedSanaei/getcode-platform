using System.Security.Cryptography;
using GetCode.Application.Identity;

namespace GetCode.Infrastructure.Identity;

/// <summary>
/// PBKDF2-HMACSHA512 password hashing (BCL-only). Stored format:
/// PBKDF2${iterations}${base64(salt)}${base64(subkey)}. Verification uses
/// CryptographicOperations.FixedTimeEquals. Iterations are raised with hardware;
/// stored hashes below the current iteration count are flagged for rehash.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    public const int SaltSizeBytes = 16;
    public const int SubkeySizeBytes = 64;

    private const string AlgorithmMarker = "PBKDF2";
    private const int CurrentIterations = 210_000;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var subkey = Rfc2898DeriveBytes.Pbkdf2(password, salt, CurrentIterations, HashAlgorithmName.SHA512, SubkeySizeBytes);
        return $"{AlgorithmMarker}${CurrentIterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(subkey)}";
    }

    public bool Verify(string password, string storedHash)
    {
        var parts = storedHash.Split('$');
        if (parts.Length != 4 || parts[0] != AlgorithmMarker)
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var iterations) || iterations < 1 || iterations > 10_000_000)
        {
            return false;
        }

        byte[] salt, expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        if (salt.Length is < 8 || expected.Length is < 32 or > 128)
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA512, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    public bool NeedsRehash(string storedHash)
    {
        var parts = storedHash.Split('$');
        return parts.Length != 4
            || parts[0] != AlgorithmMarker
            || !int.TryParse(parts[1], out var iterations)
            || iterations < CurrentIterations;
    }
}
