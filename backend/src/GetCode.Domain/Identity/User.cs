using GetCode.Domain.Common;

namespace GetCode.Domain.Identity;

/// <summary>
/// Identity owns authentication for a person. It never references wallets,
/// orders or activations; other aggregates reference the user id when needed.
/// </summary>
public sealed class User : AggregateRoot<Guid>
{
    private User(Guid id, string normalizedEmail, string passwordHash, DateTimeOffset registeredAtUtc)
        : base(id)
    {
        NormalizedEmail = normalizedEmail;
        PasswordHash = passwordHash;
        RegisteredAtUtc = registeredAtUtc;
        PasswordChangedAtUtc = registeredAtUtc;
        Status = UserStatus.Active;
    }

    /// <summary>EF materialization constructor; never use in domain code.</summary>
    private User()
        : base(Guid.Empty)
    {
        NormalizedEmail = string.Empty;
        PasswordHash = string.Empty;
        RegisteredAtUtc = default;
        PasswordChangedAtUtc = default;
    }

    public string NormalizedEmail { get; private set; }
    public string PasswordHash { get; private set; }
    public UserStatus Status { get; private set; }
    public DateTimeOffset RegisteredAtUtc { get; private set; }
    public DateTimeOffset PasswordChangedAtUtc { get; private set; }

    // Account lifecycle bookkeeping.
    public int FailedLoginCount { get; private set; }
    public DateTimeOffset? FirstFailedLoginAtUtc { get; private set; }
    public DateTimeOffset? LockedUntilUtc { get; private set; }
    public string? LockReason { get; private set; }
    public DateTimeOffset? DisabledAtUtc { get; private set; }

    /// <summary>Factory enforcing registration invariants. Email must already be normalized.</summary>
    public static User Register(string normalizedEmail, string passwordHash, DateTimeOffset nowUtc, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            throw new ArgumentException("Email is required.", nameof(normalizedEmail));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("A password hash is required; plaintext passwords never enter the domain.", nameof(passwordHash));
        }

        var user = new User(id ?? Guid.CreateVersion7(), normalizedEmail.Trim(), passwordHash, nowUtc);
        user.Raise(new UserRegistered(user.Id, user.NormalizedEmail, nowUtc));
        return user;
    }

    public bool IsLockedAt(DateTimeOffset nowUtc) =>
        Status == UserStatus.Locked
        || (LockedUntilUtc is { } until && until > nowUtc);

    public bool CanAuthenticate => Status == UserStatus.Active;

    /// <summary>
    /// Records a failed login attempt and applies lockout policy.
    /// Returns true when this attempt triggered a temporary lock.
    /// </summary>
    public bool RegisterFailedLogin(DateTimeOffset nowUtc, CredentialPolicy policy)
    {
        if (FirstFailedLoginAtUtc is null || nowUtc - FirstFailedLoginAtUtc.Value > policy.FailureWindow)
        {
            FirstFailedLoginAtUtc = nowUtc;
            FailedLoginCount = 0;
        }

        FailedLoginCount++;
        if (FailedLoginCount < policy.MaxFailedLoginsBeforeLockout)
        {
            return false;
        }

        LockedUntilUtc = nowUtc + policy.LockoutDuration;
        Raise(new UserTemporarilyLocked(Id, LockedUntilUtc.Value, nowUtc));
        return true;
    }

    public void RegisterSuccessfulLogin(DateTimeOffset nowUtc)
    {
        FailedLoginCount = 0;
        FirstFailedLoginAtUtc = null;
        LockedUntilUtc = null;
        Raise(new UserAuthenticated(Id, nowUtc));
    }

    /// <summary>Administrative/permanent lock (abuse, fraud). Distinct from temporary lockout.</summary>
    public void Lock(string reason, DateTimeOffset nowUtc)
    {
        if (Status == UserStatus.Disabled)
        {
            throw new InvalidOperationException("A disabled account cannot be locked.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A lock reason is required.", nameof(reason));
        }

        Status = UserStatus.Locked;
        LockReason = reason.Trim();
        LockedUntilUtc = null;
        Raise(new UserLockedPermanently(Id, reason.Trim(), nowUtc));
    }

    public void Unlock(DateTimeOffset nowUtc)
    {
        if (Status != UserStatus.Locked)
        {
            throw new InvalidOperationException("Only locked accounts can be unlocked.");
        }

        Status = UserStatus.Active;
        LockReason = null;
        LockedUntilUtc = null;
        FailedLoginCount = 0;
        FirstFailedLoginAtUtc = null;
        Raise(new UserUnlocked(Id, nowUtc));
    }

    public void Disable(string reason, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A disable reason is required.", nameof(reason));
        }

        Status = UserStatus.Disabled;
        DisabledAtUtc = nowUtc;
        LockReason = reason.Trim();
        Raise(new UserDisabled(Id, reason.Trim(), nowUtc));
    }

    public void SetPasswordHash(string newPasswordHash, DateTimeOffset nowUtc)
    {
        if (Status == UserStatus.Disabled)
        {
            throw new InvalidOperationException("A disabled account cannot change its password.");
        }

        if (string.IsNullOrWhiteSpace(newPasswordHash))
        {
            throw new ArgumentException("A password hash is required.", nameof(newPasswordHash));
        }

        PasswordHash = newPasswordHash;
        PasswordChangedAtUtc = nowUtc;
        FailedLoginCount = 0;
        FirstFailedLoginAtUtc = null;
        LockedUntilUtc = null;
    }
}

public enum UserStatus
{
    Active = 0,
    Locked = 1,
    Disabled = 2,
}
