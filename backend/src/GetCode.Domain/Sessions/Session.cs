using GetCode.Domain.Common;
using GetCode.Domain.Identity;

namespace GetCode.Domain.Sessions;

/// <summary>
/// A server-side login session: the only client-held secret is an opaque
/// random token whose SHA-256 hash is stored here. Sessions are scoped to one
/// site (host) — a token minted for one configured site can never authenticate
/// on another, even before cookie scoping is considered.
/// Lifetime policy lives with CredentialPolicy-adjacent constants:
/// absolute expiry, revocation is idempotent, rotation preserves user + site.
/// </summary>
public sealed class Session : AggregateRoot<Guid>
{
    private Session(
        Guid id,
        Guid userId,
        string siteKey,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
        : base(id)
    {
        UserId = userId;
        SiteKey = siteKey;
        TokenHash = tokenHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    /// <summary>EF materialization constructor; never use in domain code.</summary>
    private Session()
        : base(Guid.Empty)
    {
        UserId = Guid.Empty;
        SiteKey = string.Empty;
        TokenHash = string.Empty;
        CreatedAtUtc = default;
        ExpiresAtUtc = default;
    }

    public Guid UserId { get; private set; }
    public string SiteKey { get; private set; }
    public string TokenHash { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? RevocationReason { get; private set; }
    public Guid? RotatedFromSessionId { get; private set; }

    /// <summary>
    /// Issues a new session. The plaintext token never reaches this aggregate —
    /// callers store only its hash.
    /// </summary>
    public static Session Issue(
        Guid userId,
        string siteKey,
        string tokenHash,
        DateTimeOffset nowUtc,
        TimeSpan absoluteLifetime,
        Guid? rotatedFromSessionId = null)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user id is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(siteKey))
        {
            throw new ArgumentException("A site key is required; sessions are host-scoped by contract.", nameof(siteKey));
        }

        if (!TokenHashShape.IsValid(tokenHash))
        {
            throw new ArgumentException("The token hash must be 64 hex characters (SHA-256).", nameof(tokenHash));
        }

        if (absoluteLifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(absoluteLifetime), "Sessions must have a positive lifetime.");
        }

        var session = new Session(
            Guid.CreateVersion7(),
            userId,
            siteKey,
            tokenHash.ToLowerInvariant(),
            nowUtc,
            nowUtc + absoluteLifetime)
        {
            RotatedFromSessionId = rotatedFromSessionId,
        };
        session.Raise(new SessionIssued(session.Id, userId, siteKey, session.ExpiresAtUtc, nowUtc));
        return session;
    }

    public bool IsActive(DateTimeOffset nowUtc) => RevokedAtUtc is null && nowUtc < ExpiresAtUtc;

    /// <summary>Idempotent revocation; re-revoking keeps the original timestamp and reason.</summary>
    public void Revoke(DateTimeOffset nowUtc, string? reason = null)
    {
        if (RevokedAtUtc is not null)
        {
            return;
        }

        RevokedAtUtc = nowUtc;
        RevocationReason = reason;
        Raise(new SessionRevoked(Id, UserId, SiteKey, reason, nowUtc));
    }
}

/// <summary>Validates stored token-hash shape (SHA-256 as lowercase hex).</summary>
public static class TokenHashShape
{
    public static bool IsValid(string? tokenHash) =>
        !string.IsNullOrWhiteSpace(tokenHash)
        && tokenHash.Length == 64
        && tokenHash.All(c => c is (>= '0' and <= '9') or (>= 'a' and <= 'f') or (>= 'A' and <= 'F'));
}
