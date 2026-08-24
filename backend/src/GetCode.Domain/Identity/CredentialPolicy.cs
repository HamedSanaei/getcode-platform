namespace GetCode.Domain.Identity;

/// <summary>Credential hardening policy. Values are product constants; tests pin them.</summary>
public sealed record CredentialPolicy(
    int MaxFailedLoginsBeforeLockout,
    TimeSpan FailureWindow,
    TimeSpan LockoutDuration,
    int MinPasswordLength)
{
    public static readonly CredentialPolicy Default = new(
        MaxFailedLoginsBeforeLockout: 5,
        FailureWindow: TimeSpan.FromMinutes(15),
        LockoutDuration: TimeSpan.FromMinutes(15),
        MinPasswordLength: 12);
}

/// <summary>Pure credential-quality checks. Hashing happens behind the application port.</summary>
public static class PasswordPolicy
{
    public static IReadOnlyList<string> Validate(string? password, CredentialPolicy policy)
    {
        var violations = new List<string>();
        if (string.IsNullOrWhiteSpace(password))
        {
            violations.Add("password_required");
            return violations;
        }

        if (password.Length < policy.MinPasswordLength)
        {
            violations.Add("password_too_short");
        }

        if (!password.Any(char.IsAsciiLetterLower))
        {
            violations.Add("password_requires_lowercase");
        }

        if (!password.Any(char.IsAsciiLetterUpper))
        {
            violations.Add("password_requires_uppercase");
        }

        if (!password.Any(char.IsDigit))
        {
            violations.Add("password_requires_digit");
        }

        if (password.All(char.IsAsciiLetterOrDigit))
        {
            violations.Add("password_requires_symbol");
        }

        // Trivial sequences/repeats are rejected without a breach-corpus dependency.
        if (ContainsRunOfIdenticalCharacters(password) || ContainsSequentialRun(password))
        {
            violations.Add("password_too_predictable");
        }

        return violations;
    }

    private static bool ContainsRunOfIdenticalCharacters(string password)
    {
        for (var i = 2; i < password.Length; i++)
        {
            if (password[i] == password[i - 1] && password[i - 1] == password[i - 2])
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsSequentialRun(string password)
    {
        for (var i = 2; i < password.Length; i++)
        {
            var first = password[i - 2];
            var second = password[i - 1];
            var third = password[i];
            var ascending = second == first + 1 && third == second + 1;
            var descending = second == first - 1 && third == second - 1;
            if (ascending || descending || AreSameLetterDifferentCaseRun(first, second, third))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Catches keyboard-walk style repeats such as "aAbBcc" or "qQ" patterns of one letter.</summary>
    private static bool AreSameLetterDifferentCaseRun(char a, char b, char c) =>
        char.ToLowerInvariant(a) == char.ToLowerInvariant(b)
        && char.ToLowerInvariant(b) == char.ToLowerInvariant(c)
        && char.IsAsciiLetter(a);
}

/// <summary>Email normalization: case-insensitive addressing with a canonical stored form.</summary>
public static class EmailNormalizer
{
    public static string Normalize(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        var trimmed = email.Trim();
        var atIndex = trimmed.LastIndexOf('@');
        if (atIndex <= 0 || atIndex == trimmed.Length - 1)
        {
            throw new ArgumentException("Email format is invalid.", nameof(email));
        }

        return trimmed.ToLowerInvariant();
    }
}
