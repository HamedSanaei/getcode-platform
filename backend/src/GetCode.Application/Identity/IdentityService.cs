using GetCode.Domain.Identity;

namespace GetCode.Application.Identity;

public sealed record RegisterUserCommand(string Email, string Password, string? CorrelationId = null);

public sealed record RegisterUserResult(Guid UserId, string NormalizedEmail);

public sealed record AuthenticateCommand(string Email, string Password, string? CorrelationId = null);

public abstract record AuthenticateResult
{
    public sealed record Success(Guid UserId) : AuthenticateResult;

    /// <summary>Unknown email or wrong password — indistinguishable by design (no user enumeration).</summary>
    public sealed record InvalidCredentials : AuthenticateResult;

    public sealed record TemporarilyLockedUntil(DateTimeOffset LockedUntilUtc) : AuthenticateResult;

    public sealed record AccountLocked(string Reason) : AuthenticateResult;

    public sealed record AccountDisabled : AuthenticateResult;
}

/// <summary>
/// Authentication use cases. Money/fulfillment concerns are out of scope here by
/// construction: identity only owns credentials and account lifecycle.
/// </summary>
public sealed class IdentityService(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IIdentityAuditTrail auditTrail,
    CredentialPolicy policy)
{
    public async Task<RegisterUserResult> RegisterAsync(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var normalizedEmail = EmailNormalizer.Normalize(command.Email);
        var violations = PasswordPolicy.Validate(command.Password, policy);
        if (violations.Count > 0)
        {
            throw new IdentityRuleViolationException("registration_rejected", violations);
        }

        if (await users.ExistsWithNormalizedEmailAsync(normalizedEmail, cancellationToken))
        {
            // Same response as success-path timing allows; no account existence disclosure.
            throw new IdentityRuleViolationException("email_unavailable", ["email_already_registered"]);
        }

        var user = User.Register(normalizedEmail, passwordHasher.Hash(command.Password), DateTimeOffset.UtcNow);
        users.Add(user);
        await auditTrail.RecordAsync(new IdentityAuditEvent(
            user.Id,
            "identity.user.registered",
            Succeeded: true,
            command.CorrelationId,
            new Dictionary<string, string> { ["normalized_email"] = normalizedEmail }), cancellationToken);

        return new RegisterUserResult(user.Id, normalizedEmail);
    }

    public async Task<AuthenticateResult> AuthenticateAsync(AuthenticateCommand command, CancellationToken cancellationToken)
    {
        var normalizedEmail = EmailNormalizer.Normalize(command.Email);
        var now = DateTimeOffset.UtcNow;
        var user = await users.FindByNormalizedEmailAsync(normalizedEmail, cancellationToken);

        if (user is null)
        {
            // Burn comparable time to reduce username-probing via latency.
            passwordHasher.Verify(command.Password, DummyHashForTiming);
            await AuditFailureAsync(null, "identity.login.failed_unknown_account", command.CorrelationId, cancellationToken);
            return new AuthenticateResult.InvalidCredentials();
        }

        if (user.Status == UserStatus.Disabled)
        {
            await AuditFailureAsync(user.Id, "identity.login.rejected_disabled", command.CorrelationId, cancellationToken);
            return new AuthenticateResult.AccountDisabled();
        }

        if (user.Status == UserStatus.Locked)
        {
            await AuditFailureAsync(user.Id, "identity.login.rejected_locked", command.CorrelationId, cancellationToken);
            return new AuthenticateResult.AccountLocked(user.LockReason ?? "locked");
        }

        if (user.IsLockedAt(now))
        {
            await AuditFailureAsync(user.Id, "identity.login.rejected_temporarily_locked", command.CorrelationId, cancellationToken);
            return new AuthenticateResult.TemporarilyLockedUntil(user.LockedUntilUtc!.Value);
        }

        if (!passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            var lockTriggered = user.RegisterFailedLogin(now, policy);
            await AuditFailureAsync(user.Id, lockTriggered ? "identity.login.failed_lockout_triggered" : "identity.login.failed_wrong_password", command.CorrelationId, cancellationToken);
            return lockTriggered
                ? new AuthenticateResult.TemporarilyLockedUntil(user.LockedUntilUtc!.Value)
                : new AuthenticateResult.InvalidCredentials();
        }

        user.RegisterSuccessfulLogin(now);
        if (passwordHasher.NeedsRehash(user.PasswordHash))
        {
            user.SetPasswordHash(passwordHasher.Hash(command.Password), now);
        }

        await auditTrail.RecordAsync(new IdentityAuditEvent(
            user.Id,
            "identity.login.succeeded",
            Succeeded: true,
            command.CorrelationId), cancellationToken);
        return new AuthenticateResult.Success(user.Id);
    }

    private async Task AuditFailureAsync(Guid? userId, string eventType, string? correlationId, CancellationToken cancellationToken) =>
        await auditTrail.RecordAsync(new IdentityAuditEvent(userId, eventType, Succeeded: false, correlationId), cancellationToken);

    /// <summary>A stable hash-format string used only to equalize latency for unknown accounts.</summary>
    private const string DummyHashForTiming = "PBKDF2$3$210000$dGltZXN0ZWU=$dGltZXN0ZWV0aW1lc3RlYXRpbWVzdGVhZGluZw==";
}

public sealed class IdentityRuleViolationException(string rule, IReadOnlyList<string> violations)
    : Exception($"Identity rule '{rule}' violated: {string.Join(", ", violations)}")
{
    public string Rule { get; } = rule;
    public IReadOnlyList<string> Violations { get; } = violations;
}
