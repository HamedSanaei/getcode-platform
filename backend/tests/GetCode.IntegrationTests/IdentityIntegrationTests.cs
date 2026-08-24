using GetCode.Application.Identity;
using GetCode.IntegrationTests.Infrastructure;
using GetCode.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GetCode.IntegrationTests;

/// <summary>
/// M02-001 verification: identity flows against real PostgreSQL — uniqueness
/// enforcement, durable lockout state, and audit events that never contain
/// secrets. Uses the production composition root unchanged.
/// </summary>
[Collection(DatabaseCollection.CollectionName)]
public sealed class IdentityIntegrationTests(DatabaseFixture database)
{
    private const string StrongPassword = "correct-horse-Battery7";

    /// <summary>Owns factory + scope lifetime for the duration of a test.</summary>
    private sealed class IdentityServiceScope : IAsyncDisposable
    {
        private readonly GetCodeApiFactory _factory;
        private readonly IServiceScope _scope;

        public IdentityServiceScope(DatabaseFixture databaseFixture)
        {
            _factory = new GetCodeApiFactory(databaseFixture);
            _scope = _factory.Services.CreateScope();
        }

        public IdentityService Service => _scope.ServiceProvider.GetRequiredService<IdentityService>();

        public async ValueTask DisposeAsync()
        {
            await ((IAsyncDisposable)_scope).DisposeAsync();
            await _factory.DisposeAsync();
        }
    }

    private IdentityServiceScope CreateService() => new(database);

    [Fact]
    public async Task Registered_user_is_persisted_with_normalized_unique_email()
    {
        await using var serviceScope = CreateService();
        var result = await serviceScope.Service.RegisterAsync(
            new RegisterUserCommand("Person@Example.com", StrongPassword, "corr-reg"), TestContext.Current.CancellationToken);

        Assert.Equal("person@example.com", result.NormalizedEmail);

        // Case-insensitive duplicate (same normalized address) must be refused.
        var violation = await Assert.ThrowsAsync<IdentityRuleViolationException>(
            () => serviceScope.Service.RegisterAsync(new RegisterUserCommand("PERSON@EXAMPLE.COM", "another-Password8!"), TestContext.Current.CancellationToken));
        Assert.Equal("email_unavailable", violation.Rule);
    }

    [Fact]
    public async Task Lockout_survives_process_restart_via_database_state()
    {
        await using (var setup = CreateService())
        {
            await setup.Service.RegisterAsync(
                new RegisterUserCommand("lockout@example.com", StrongPassword), TestContext.Current.CancellationToken);
        }

        AuthenticateResult last = new AuthenticateResult.InvalidCredentials();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            // Fresh scope per attempt simulates process restarts between logins.
            await using var failing = CreateService();
            last = await failing.Service.AuthenticateAsync(
                new AuthenticateCommand("lockout@example.com", "totally-wrong-Pw1!"), TestContext.Current.CancellationToken);
        }

        Assert.IsType<AuthenticateResult.TemporarilyLockedUntil>(last);

        // A brand-new service instance sees the durable lock.
        await using var verifier = CreateService();
        var whileLocked = await verifier.Service.AuthenticateAsync(
            new AuthenticateCommand("lockout@example.com", StrongPassword), TestContext.Current.CancellationToken);
        Assert.IsType<AuthenticateResult.TemporarilyLockedUntil>(whileLocked);

        // The user row carries the lock window.
        var options = new DbContextOptionsBuilder<GetCodeDbContext>().UseNpgsql(database.ConnectionString).Options;
        await using var context = new GetCodeDbContext(options);
        var user = await context.Users.SingleAsync(u => u.NormalizedEmail == "lockout@example.com", TestContext.Current.CancellationToken);
        Assert.NotNull(user.LockedUntilUtc);
        Assert.True(user.FailedLoginCount >= 5);
    }

    [Fact]
    public async Task Audit_events_are_written_without_secrets()
    {
        await using (var setup = CreateService())
        {
            await setup.Service.RegisterAsync(new RegisterUserCommand("audit@example.com", StrongPassword), TestContext.Current.CancellationToken);
        }

        await using (var attempts = CreateService())
        {
            await attempts.Service.AuthenticateAsync(new AuthenticateCommand("audit@example.com", "wrong-password-Q2@"), TestContext.Current.CancellationToken);
            await attempts.Service.AuthenticateAsync(new AuthenticateCommand("audit@example.com", StrongPassword), TestContext.Current.CancellationToken);
        }

        var options = new DbContextOptionsBuilder<GetCodeDbContext>().UseNpgsql(database.ConnectionString).Options;
        await using var context = new GetCodeDbContext(options);
        var auditRows = await context.IdentityAuditEvents
            .Where(e => e.EventType!.StartsWith("identity."))
            .OrderBy(e => e.OccurredAtUtc)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Contains(auditRows, e => e.EventType == "identity.user.registered");
        Assert.Contains(auditRows, e => e.EventType == "identity.login.failed_wrong_password");
        Assert.Contains(auditRows, e => e.EventType == "identity.login.succeeded");

        // No secret material anywhere in the audit payload.
        foreach (var row in auditRows)
        {
            Assert.DoesNotContain(StrongPassword, row.DetailsJson ?? string.Empty);
            Assert.DoesNotContain("PBKDF2$", row.DetailsJson ?? string.Empty);
        }
    }

    [Fact]
    public async Task Audit_adapter_refuses_sensitive_detail_keys()
    {
        await using var factory = new GetCodeApiFactory(database);
        using var scope = factory.Services.CreateScope();
        var trail = scope.ServiceProvider.GetRequiredService<IIdentityAuditTrail>();

        var leakAttempt = new IdentityAuditEvent(
            null,
            "identity.test.probe",
            Succeeded: false,
            CorrelationId: null,
            Details: new Dictionary<string, string> { ["password"] = "must-not-persist" });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            trail.RecordAsync(leakAttempt, TestContext.Current.CancellationToken));
    }
}
