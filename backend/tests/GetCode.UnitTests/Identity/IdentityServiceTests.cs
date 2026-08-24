using GetCode.Application.Identity;
using GetCode.Domain.Identity;
using GetCode.Infrastructure.Identity;

namespace GetCode.UnitTests;

/// <summary>
/// M02-001: credential policy, lockout workflow and audit semantics through the
/// authentication service with deterministic fakes. Secrets must never reach
/// the audit trail.
/// </summary>
public sealed class IdentityServiceTests
{
    private static readonly DateTimeOffset Start = new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeUserRepository _users = new();
    private readonly FakeAuditTrail _audit = new();
    private readonly IdentityService _service;

    public IdentityServiceTests()
    {
        _service = new IdentityService(_users, new Pbkdf2PasswordHasher(), _audit, CredentialPolicy.Default);
    }

    [Fact]
    public async Task Register_hashes_password_and_audits_without_secrets()
    {
        var result = await _service.RegisterAsync(new RegisterUserCommand("User@Example.com", "correct-horse-Battery7"), TestContext.Current.CancellationToken);

        Assert.Equal("user@example.com", result.NormalizedEmail);
        var stored = await _users.FindByNormalizedEmailAsync("user@example.com", TestContext.Current.CancellationToken);
        Assert.NotNull(stored);
        Assert.NotEqual("correct-horse-Battery7", stored!.PasswordHash);
        Assert.StartsWith("PBKDF2$", stored.PasswordHash);

        var auditEvent = Assert.Single(_audit.Events);
        Assert.Equal("identity.user.registered", auditEvent.EventType);
        Assert.True(auditEvent.Succeeded);
        Assert.DoesNotContain("correct-horse-Battery7", auditEvent.Details?["normalized_email"] ?? string.Empty);
    }

    [Fact]
    public async Task Register_rejects_weak_passwords()
    {
        await Assert.ThrowsAsync<IdentityRuleViolationException>(
            () => _service.RegisterAsync(new RegisterUserCommand("user@example.com", "short"), TestContext.Current.CancellationToken));
        Assert.Empty(_users.All);
        Assert.Empty(_audit.Events);
    }

    [Fact]
    public async Task Register_rejects_duplicate_email_without_disclosing_more()
    {
        await _service.RegisterAsync(new RegisterUserCommand("user@example.com", "correct-horse-Battery7"), TestContext.Current.CancellationToken);

        var violation = await Assert.ThrowsAsync<IdentityRuleViolationException>(
            () => _service.RegisterAsync(new RegisterUserCommand("USER@Example.com", "another-Password9!"), TestContext.Current.CancellationToken));

        Assert.Equal("email_unavailable", violation.Rule);
        Assert.Single(_users.All);
    }

    [Fact]
    public async Task Authenticate_success_returns_user_and_resets_failures()
    {
        var registration = await _service.RegisterAsync(new RegisterUserCommand("user@example.com", "correct-horse-Battery7"), TestContext.Current.CancellationToken);

        var result = await _service.AuthenticateAsync(new AuthenticateCommand("USER@example.com", "correct-horse-Battery7"), TestContext.Current.CancellationToken);

        var success = Assert.IsType<AuthenticateResult.Success>(result);
        Assert.Equal(registration.UserId, success.UserId);
        Assert.Contains(_audit.Events, e => e.EventType == "identity.login.succeeded");
    }

    [Fact]
    public async Task Wrong_password_locks_account_at_threshold()
    {
        await _service.RegisterAsync(new RegisterUserCommand("user@example.com", "correct-horse-Battery7"), TestContext.Current.CancellationToken);

        AuthenticateResult result = new AuthenticateResult.InvalidCredentials();
        for (var attempt = 0; attempt < CredentialPolicy.Default.MaxFailedLoginsBeforeLockout; attempt++)
        {
            result = await _service.AuthenticateAsync(new AuthenticateCommand("user@example.com", "wrong-password-X1!"), TestContext.Current.CancellationToken);
        }

        Assert.IsType<AuthenticateResult.TemporarilyLockedUntil>(result);

        // Even the correct password is refused while locked.
        var whileLocked = await _service.AuthenticateAsync(new AuthenticateCommand("user@example.com", "correct-horse-Battery7"), TestContext.Current.CancellationToken);
        Assert.IsType<AuthenticateResult.TemporarilyLockedUntil>(whileLocked);

        Assert.Contains(_audit.Events, e => e.EventType == "identity.login.failed_lockout_triggered");
    }

    [Fact]
    public async Task Unknown_account_is_indistinguishable_from_wrong_password()
    {
        var knownAccountResult = await _service.AuthenticateAsync(new AuthenticateCommand("nobody@example.com", "whatever-Password1!"), TestContext.Current.CancellationToken);

        Assert.IsType<AuthenticateResult.InvalidCredentials>(knownAccountResult);
        var auditEvent = Assert.Single(_audit.Events);
        Assert.Null(auditEvent.UserId);
        Assert.False(auditEvent.Succeeded);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly Dictionary<string, User> _byEmail = [];

        public IReadOnlyCollection<User> All => _byEmail.Values;

        public Task<User?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
            Task.FromResult(_byEmail.GetValueOrDefault(normalizedEmail));

        public Task<User?> FindByIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(_byEmail.Values.FirstOrDefault(u => u.Id == userId));

        public Task<bool> ExistsWithNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
            Task.FromResult(_byEmail.ContainsKey(normalizedEmail));

        public void Add(User user) => _byEmail[user.NormalizedEmail] = user;
    }

    private sealed class FakeAuditTrail : IIdentityAuditTrail
    {
        public List<IdentityAuditEvent> Events { get; } = [];

        public Task RecordAsync(IdentityAuditEvent auditEvent, CancellationToken cancellationToken)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }
}
