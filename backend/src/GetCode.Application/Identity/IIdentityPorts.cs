using GetCode.Domain.Identity;

namespace GetCode.Application.Identity;

/// <summary>Hashing port; implementations live in Infrastructure. Never logs or exposes plaintext.</summary>
public interface IPasswordHasher
{
    /// <summary>Returns a self-describing hash string (algorithm + parameters + salt + digest).</summary>
    string Hash(string password);

    /// <summary>Constant-time verification against a stored hash string.</summary>
    bool Verify(string password, string storedHash);

    /// <summary>True when the stored hash uses weaker parameters than the current policy (rehash on next login).</summary>
    bool NeedsRehash(string storedHash);
}

public interface IUserRepository
{
    Task<User?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task<User?> FindByIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> ExistsWithNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    void Add(User user);
}

/// <summary>
/// Durable audit sink for security-relevant identity events. Implementations must persist
/// structured metadata only — never passwords, hashes, tokens or OTP values.
/// </summary>
public interface IIdentityAuditTrail
{
    Task RecordAsync(IdentityAuditEvent auditEvent, CancellationToken cancellationToken);
}

public sealed record IdentityAuditEvent(
    Guid? UserId,
    string EventType,
    bool Succeeded,
    string? CorrelationId,
    IReadOnlyDictionary<string, string>? Details = null)
{
    public DateTimeOffset OccurredAtUtc { get; } = DateTimeOffset.UtcNow;
}
