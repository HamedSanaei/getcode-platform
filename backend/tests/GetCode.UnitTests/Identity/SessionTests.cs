using GetCode.Application.Identity;
using GetCode.Domain.Identity;
using GetCode.Domain.Sessions;

namespace GetCode.UnitTests.Identity;

/// <summary>M02-002: session aggregate and service rules with in-memory fakes.</summary>
public sealed class SessionTests
{
    private const string ValidHash = "a2fd0a5d8541e0f8bb3cdd7bd8b6f5b1a4c9d2f7e8b3a1c0d9e8f7a6b5c4d321";

    [Fact]
    public void Issue_rejects_missing_site_keys_and_bad_hashes()
    {
        // Site-key vocabulary is a service-level concern; the domain enforces presence and shape.
        Assert.Throws<ArgumentException>(() => Session.Issue(Guid.NewGuid(), "", ValidHash, DateTimeOffset.UtcNow, TimeSpan.FromDays(1)));
        Assert.Throws<ArgumentException>(() => Session.Issue(Guid.NewGuid(), "primary", "short-hash", DateTimeOffset.UtcNow, TimeSpan.FromDays(1)));
        Assert.Throws<ArgumentException>(() => Session.Issue(Guid.Empty, "primary", ValidHash, DateTimeOffset.UtcNow, TimeSpan.FromDays(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => Session.Issue(Guid.NewGuid(), "primary", ValidHash, DateTimeOffset.UtcNow, TimeSpan.Zero));
    }

    [Fact]
    public void Session_is_active_until_expiry_and_revocation_is_idempotent()
    {
        var now = DateTimeOffset.UtcNow;
        var session = Session.Issue(Guid.NewGuid(), "primary", ValidHash, now, TimeSpan.FromDays(7));

        Assert.True(session.IsActive(now));
        Assert.True(session.IsActive(now.AddDays(6)));
        Assert.False(session.IsActive(now.AddDays(8))); // absolute lifetime

        session.Revoke(now.AddMinutes(5), "logout");
        Assert.False(session.IsActive(now.AddMinutes(6)));

        // Idempotent: original timestamp/reason survive a second revoke.
        session.Revoke(now.AddMinutes(7), "again");
        Assert.Equal(now.AddMinutes(5), session.RevokedAtUtc);
        Assert.Equal("logout", session.RevocationReason);
    }

    [Fact]
    public async Task Validation_enforces_site_scoping_server_side()
    {
        var service = CreateService(out var tokens, out var repo);

        var issued = await service.IssueAsync(Guid.NewGuid(), "pluspremium", CancellationToken.None);

        Assert.IsType<SessionValidationResult.Success>(await service.ValidateAsync(issued.Token, "pluspremium", CancellationToken.None));
        // The same token presented to the other site must fail even without cookies involved.
        Assert.IsType<SessionValidationResult.SiteMismatch>(await service.ValidateAsync(issued.Token, "primary", CancellationToken.None));
        Assert.NotNull(tokens);
        Assert.NotNull(repo);
    }

    [Fact]
    public async Task Rotation_revokes_the_old_token_and_preserves_user_and_site()
    {
        var service = CreateService(out _, out _);
        var userId = Guid.NewGuid();
        var original = await service.IssueAsync(userId, "primary", CancellationToken.None);

        var replacement = await service.RotateAsync(original.Token, "primary", CancellationToken.None);

        Assert.NotNull(replacement);
        Assert.Equal(userId, replacement!.UserId);
        Assert.Equal("primary", replacement.SiteKey);
        Assert.NotEqual(original.Token, replacement.Token);
        Assert.NotEqual(original.SessionId, replacement.SessionId);

        Assert.IsType<SessionValidationResult.Revoked>(await service.ValidateAsync(original.Token, "primary", CancellationToken.None));
        Assert.IsType<SessionValidationResult.Success>(await service.ValidateAsync(replacement.Token, "primary", CancellationToken.None));

        // Rotation cannot be hijacked cross-site.
        Assert.Null(await service.RotateAsync(replacement.Token, "pluspremium", CancellationToken.None));
    }

    [Fact]
    public async Task RevokeAllForUser_spans_all_sites()
    {
        var service = CreateService(out _, out var repo);
        var userId = Guid.NewGuid();
        var primary = await service.IssueAsync(userId, "primary", CancellationToken.None);
        var premium = await service.IssueAsync(userId, "pluspremium", CancellationToken.None);

        var revokedCount = await service.RevokeAllForUserAsync(userId, "password_changed", CancellationToken.None);

        Assert.Equal(2, revokedCount);
        Assert.IsType<SessionValidationResult.Revoked>(await service.ValidateAsync(primary.Token, "primary", CancellationToken.None));
        Assert.IsType<SessionValidationResult.Revoked>(await service.ValidateAsync(premium.Token, "pluspremium", CancellationToken.None));
        Assert.Equal(0, (repo as FakeSessionRepository)!.Saved.Count(s => s.Id == Guid.Empty));
    }

    [Fact]
    public async Task Issue_rejects_unknown_sites()
    {
        var service = CreateService(out _, out _);
        await Assert.ThrowsAsync<IdentityRuleViolationException>(
            () => service.IssueAsync(Guid.NewGuid(), "attacker-site", CancellationToken.None));
    }

    private static SessionService CreateService(out FakeTokenProvider tokens, out ISessionRepository repo)
    {
        tokens = new FakeTokenProvider();
        repo = new FakeSessionRepository();
        return new SessionService(repo, tokens, new RecordingAuditTrail(), new FakeUserRepository());
    }

    private sealed class FakeTokenProvider : ISessionTokenProvider
    {
        private int _counter;
        public string CreateToken() => $"token-{Interlocked.Increment(ref _counter)}-{Guid.NewGuid():N}";
        public string HashToken(string token)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(token);
            var digest = System.Security.Cryptography.SHA256.HashData(bytes);
            return Convert.ToHexString(digest).ToLowerInvariant();
        }
    }

    private sealed class FakeSessionRepository : ISessionRepository
    {
        public List<Session> Saved { get; } = [];

        public Task<Session?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
            Task.FromResult(Saved.FirstOrDefault(s => s.TokenHash == tokenHash));

        public Task<Session?> FindByIdAsync(Guid sessionId, CancellationToken cancellationToken) =>
            Task.FromResult(Saved.FirstOrDefault(s => s.Id == sessionId));

        public Task<IReadOnlyList<Session>> ListActiveForUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            IReadOnlyList<Session> active = Saved
                .Where(s => s.UserId == userId && s.RevokedAtUtc is null && s.ExpiresAtUtc > DateTimeOffset.UtcNow)
                .ToList();
            return Task.FromResult(active);
        }

        public void Add(Session session) => Saved.Add(session);
    }

    private sealed class RecordingAuditTrail : IIdentityAuditTrail
    {
        public List<IdentityAuditEvent> Events { get; } = [];
        public Task RecordAsync(IdentityAuditEvent auditEvent, CancellationToken cancellationToken)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public Task<User?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken) => Task.FromResult<User?>(null);
        public Task<User?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            var user = User.Register("session-user@example.com", "PBKDF2$3$210000$c2FsdA==$c2FsdHNhbHRzYWx0c2FsdA==", DateTimeOffset.UtcNow, userId);
            user.ClearDomainEvents();
            return Task.FromResult<User?>(user);
        }
        public Task<bool> ExistsWithNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken) => Task.FromResult(false);
        public void Add(User user) { }
    }
}
