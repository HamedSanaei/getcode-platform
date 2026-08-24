using GetCode.Domain.Identity;

namespace GetCode.UnitTests;

public sealed class UserTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);
    private const string Hash = "PBKDF2$210000$c2FsdA==$ZGl nZXN0";

    [Fact]
    public void Register_creates_active_user_with_uuidv7_identity_and_event()
    {
        var user = User.Register("user@example.com", "hash-value", Now);

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal(7, user.Id.Version);
        Assert.Equal(UserStatus.Active, user.Status);
        Assert.Equal("user@example.com", user.NormalizedEmail);
        Assert.Equal("hash-value", user.PasswordHash);
        Assert.Single(user.DomainEvents.OfType<UserRegistered>());
    }

    [Theory]
    [InlineData(null, "hash")]
    [InlineData("", "hash")]
    [InlineData("  ", "hash")]
    public void Register_rejects_missing_email(string? email, string hash)
    {
        Assert.ThrowsAny<ArgumentException>(() => User.Register(email!, hash, Now));
    }

    [Fact]
    public void Register_rejects_plaintext_shortcut()
    {
        // A missing hash is rejected: plaintext passwords never enter the domain.
        Assert.ThrowsAny<ArgumentException>(() => User.Register("user@example.com", "", Now));
    }

    [Fact]
    public void Failed_logins_trigger_lockout_at_policy_threshold()
    {
        var user = User.Register("user@example.com", Hash, Now);

        var locked = false;
        for (var attempt = 1; attempt <= CredentialPolicy.Default.MaxFailedLoginsBeforeLockout; attempt++)
        {
            locked = user.RegisterFailedLogin(Now.AddMinutes(attempt), CredentialPolicy.Default);
        }

        Assert.True(locked);
        Assert.True(user.IsLockedAt(Now.AddMinutes(6)));
        Assert.Contains(user.DomainEvents, e => e is UserTemporarilyLocked);

        // After the lockout window the account accepts authentication again.
        var lockExpiry = user.LockedUntilUtc!.Value;
        Assert.False(user.IsLockedAt(lockExpiry.AddSeconds(1)));
    }

    [Fact]
    public void Failure_window_reset_lets_slow_attempts_avoid_lockout()
    {
        var user = User.Register("user@example.com", Hash, Now);
        var policy = CredentialPolicy.Default;

        for (var attempt = 1; attempt <= policy.MaxFailedLoginsBeforeLockout - 1; attempt++)
        {
            user.RegisterFailedLogin(Now.AddMinutes(attempt), policy);
        }

        // Next attempt happens after the whole failure window → counter restarts.
        var afterWindow = Now.AddMinutes(policy.FailureWindow.TotalMinutes + 2);
        var lockedNow = user.RegisterFailedLogin(afterWindow, policy);

        Assert.False(lockedNow);
        Assert.Equal(1, user.FailedLoginCount);
    }

    [Fact]
    public void Successful_login_clears_failure_state()
    {
        var user = User.Register("user@example.com", Hash, Now);
        user.RegisterFailedLogin(Now, CredentialPolicy.Default);
        user.RegisterFailedLogin(Now.AddSeconds(30), CredentialPolicy.Default);

        user.RegisterSuccessfulLogin(Now.AddMinutes(1));

        Assert.Equal(0, user.FailedLoginCount);
        Assert.Null(user.FirstFailedLoginAtUtc);
        Assert.True(user.CanAuthenticate);
    }

    [Fact]
    public void Permanent_lock_is_distinct_from_temporary_lockout()
    {
        var user = User.Register("user@example.com", Hash, Now);

        user.Lock("fraud investigation", Now.AddHours(1));

        Assert.Equal(UserStatus.Locked, user.Status);
        Assert.Null(user.LockedUntilUtc);
        Assert.Equal("fraud investigation", user.LockReason);
        // A permanent lock never expires on its own...
        Assert.True(user.IsLockedAt(Now.AddYears(1)));

        // ...but an administrator can lift it explicitly.
        user.Unlock(Now.AddDays(2));
        Assert.Equal(UserStatus.Active, user.Status);
    }

    [Fact]
    public void Unlock_restores_active_status()
    {
        var user = User.Register("user@example.com", Hash, Now);
        user.Lock("abuse", Now);

        user.Unlock(Now.AddDays(1));

        Assert.Equal(UserStatus.Active, user.Status);
        Assert.Null(user.LockReason);
    }

    [Fact]
    public void Disabled_account_cannot_authenticate_or_transition()
    {
        var user = User.Register("user@example.com", Hash, Now);

        user.Disable("gdpr erasure request", Now);

        Assert.Equal(UserStatus.Disabled, user.Status);
        Assert.False(user.CanAuthenticate);
        Assert.Throws<InvalidOperationException>(() => user.Lock("x", Now));
        Assert.Throws<InvalidOperationException>(() => user.SetPasswordHash("new-hash", Now));
    }
}

public sealed class PasswordPolicyTests
{
    private static readonly CredentialPolicy Policy = CredentialPolicy.Default;

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_password_is_rejected(string? password)
    {
        Assert.Contains("password_required", PasswordPolicy.Validate(password, Policy));
    }

    [Fact]
    public void Short_password_is_rejected()
    {
        Assert.Contains("password_too_short", PasswordPolicy.Validate("Sh0rt!x", Policy));
    }

    [Fact]
    public void Each_missing_category_has_a_distinct_violation()
    {
        var allLower = PasswordPolicy.Validate("abcdefghijklm!", Policy);
        Assert.Contains("password_requires_uppercase", allLower);
        Assert.Contains("password_requires_digit", allLower);

        var noSymbol = PasswordPolicy.Validate("Abcdefgh123", Policy);
        Assert.Contains("password_requires_symbol", noSymbol);
    }

    [Theory]
    [InlineData("Password111!aaa")]
    [InlineData("abcdefghijkL1!")]
    public void Predictable_passwords_are_rejected(string password)
    {
        Assert.Contains("password_too_predictable", PasswordPolicy.Validate(password, Policy));
    }

    [Fact]
    public void Strong_passphrase_passes()
    {
        Assert.Empty(PasswordPolicy.Validate("correct-horse-Battery7", Policy));
    }
}

public sealed class EmailNormalizerTests
{
    [Theory]
    [InlineData("User@Example.COM ", "user@example.com")]
    [InlineData("  mixed.CASE@Mail.example.org", "mixed.case@mail.example.org")]
    public void Emails_are_trimmed_and_lowercased(string input, string expected)
    {
        Assert.Equal(expected, EmailNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-at-sign")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    public void Invalid_emails_throw(string email)
    {
        Assert.ThrowsAny<ArgumentException>(() => EmailNormalizer.Normalize(email));
    }
}
