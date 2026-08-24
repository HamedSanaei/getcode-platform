using GetCode.Domain.Sessions;

namespace GetCode.Application.Identity;

/// <summary>
/// Opaque session token material. Implementations (Infrastructure) must use a
/// cryptographically secure generator; only the hash is ever persisted.
/// </summary>
public interface ISessionTokenProvider
{
    /// <summary>≥256 bits of entropy, url-safe, safe as a cookie value.</summary>
    string CreateToken();

    /// <summary>SHA-256 hex digest of the token — the value stored server-side.</summary>
    string HashToken(string token);
}

public interface ISessionRepository
{
    Task<Session?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task<Session?> FindByIdAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Session>> ListActiveForUserAsync(Guid userId, CancellationToken cancellationToken);
    void Add(Session session);
}

public sealed record IssuedSession(Guid SessionId, Guid UserId, string SiteKey, string Token, DateTimeOffset ExpiresAtUtc);

public abstract record SessionValidationResult
{
    public sealed record Success(Guid SessionId, Guid UserId) : SessionValidationResult;

    public sealed record NotFound : SessionValidationResult;

    /// <summary>Token belongs to another configured site — host scoping enforced server-side.</summary>
    public sealed record SiteMismatch : SessionValidationResult;

    public sealed record Expired : SessionValidationResult;

    public sealed record Revoked : SessionValidationResult;
}

/// <summary>
/// Server-side session use cases. Policy (documented for M02-002):
/// - Opaque 256-bit tokens in cookies; the database stores only SHA-256 hashes.
/// - Absolute lifetime (no sliding renewal): re-login is required after expiry.
/// - Each login issues a fresh session; existing sessions on other devices stay valid.
/// - Rotation replaces exactly one exposed session and preserves user + site.
/// - Revocation is immediate, per-session or all-sessions-for-user.
/// </summary>
public sealed class SessionService(
    ISessionRepository sessions,
    ISessionTokenProvider tokens,
    IIdentityAuditTrail auditTrail,
    IUserRepository users)
{
    public const string PrimarySiteKey = "primary";
    public const string PlusPremiumSiteKey = "pluspremium";
    public static readonly TimeSpan AbsoluteLifetime = TimeSpan.FromDays(7);

    public static bool IsKnownSiteKey(string siteKey) =>
        siteKey is PrimarySiteKey or PlusPremiumSiteKey;

    public async Task<IssuedSession> IssueAsync(Guid userId, string siteKey, CancellationToken cancellationToken)
    {
        if (!IsKnownSiteKey(siteKey))
        {
            throw new IdentityRuleViolationException("session_site_unknown", [siteKey]);
        }

        var user = await users.FindByIdAsync(userId, cancellationToken)
            ?? throw new IdentityRuleViolationException("session_user_unknown", ["user_not_found"]);
        if (!user.CanAuthenticate)
        {
            throw new IdentityRuleViolationException("session_user_not_active", ["user_not_active"]);
        }

        var now = DateTimeOffset.UtcNow;
        var token = tokens.CreateToken();
        var session = Session.Issue(user.Id, siteKey, tokens.HashToken(token), now, AbsoluteLifetime);
        sessions.Add(session);

        await auditTrail.RecordAsync(new IdentityAuditEvent(
            userId,
            "identity.session.issued",
            Succeeded: true,
            CorrelationId: null,
            new Dictionary<string, string> { ["site_key"] = siteKey }), cancellationToken);

        return new IssuedSession(session.Id, userId, siteKey, token, session.ExpiresAtUtc);
    }

    public async Task<SessionValidationResult> ValidateAsync(string? token, string siteKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return new SessionValidationResult.NotFound();
        }

        var session = await sessions.FindByTokenHashAsync(tokens.HashToken(token), cancellationToken);
        if (session is null)
        {
            return new SessionValidationResult.NotFound();
        }

        if (!string.Equals(session.SiteKey, siteKey, StringComparison.Ordinal))
        {
            return new SessionValidationResult.SiteMismatch();
        }

        if (session.RevokedAtUtc is not null)
        {
            return new SessionValidationResult.Revoked();
        }

        if (DateTimeOffset.UtcNow >= session.ExpiresAtUtc)
        {
            return new SessionValidationResult.Expired();
        }

        return new SessionValidationResult.Success(session.Id, session.UserId);
    }

    /// <summary>
    /// Replaces one active session with a fresh one for the same user + site.
    /// Used when a token may have been exposed (shared device, header leak).
    /// The old token stops working immediately.
    /// </summary>
    public async Task<IssuedSession?> RotateAsync(string token, string siteKey, CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(token, siteKey, cancellationToken);
        if (validation is not SessionValidationResult.Success success)
        {
            return null;
        }

        var old = await sessions.FindByIdAsync(success.SessionId, cancellationToken)
            ?? throw new InvalidOperationException($"Rotated session {success.SessionId} could not be loaded.");
        var replacement = await IssueAsync(old.UserId, old.SiteKey, cancellationToken);
        old.Revoke(DateTimeOffset.UtcNow, reason: "rotated");

        await auditTrail.RecordAsync(new IdentityAuditEvent(
            old.UserId,
            "identity.session.rotated",
            Succeeded: true,
            CorrelationId: null,
            new Dictionary<string, string> { ["site_key"] = old.SiteKey }), cancellationToken);

        return replacement;
    }

    public async Task<bool> RevokeAsync(string token, CancellationToken cancellationToken)
    {
        var session = await sessions.FindByTokenHashAsync(tokens.HashToken(token), cancellationToken);
        if (session is null)
        {
            return false;
        }

        session.Revoke(DateTimeOffset.UtcNow, reason: "logout");
        await auditTrail.RecordAsync(new IdentityAuditEvent(
            session.UserId,
            "identity.session.revoked",
            Succeeded: true,
            CorrelationId: null,
            new Dictionary<string, string> { ["site_key"] = session.SiteKey }), cancellationToken);
        return true;
    }

    /// <summary>Revokes every live session for the user across all sites (e.g., password change).</summary>
    public async Task<int> RevokeAllForUserAsync(Guid userId, string reason, CancellationToken cancellationToken)
    {
        var active = await sessions.ListActiveForUserAsync(userId, cancellationToken);
        var revoked = 0;
        foreach (var session in active)
        {
            session.Revoke(DateTimeOffset.UtcNow, reason);
            revoked++;
        }

        if (revoked > 0)
        {
            await auditTrail.RecordAsync(new IdentityAuditEvent(
                userId,
                "identity.session.revoked_all",
                Succeeded: true,
                CorrelationId: null,
                new Dictionary<string, string> { ["count"] = revoked.ToString(System.Globalization.CultureInfo.InvariantCulture) }),
                cancellationToken);
        }

        return revoked;
    }
}
