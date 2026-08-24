using GetCode.Application.Authorization;
using GetCode.Application.Catalog;
using GetCode.Application.Identity;
using GetCode.Domain.Authorization;
using GetCode.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GetCode.IntegrationTests;

/// <summary>
/// M02-004 verification: the server-side authorization matrix. Deny-by-default
/// effective permissions, role-scoped grants, revocation propagation, and
/// audit events for every privilege change.
/// </summary>
[Collection(DatabaseCollection.CollectionName)]
public sealed class AuthorizationMatrixTests(DatabaseFixture database)
{
    [Fact]
    public async Task Authorization_matrix_enforces_deny_by_default_and_role_scoping()
    {
        await using var scope = new AuthorizationScope(database);
        var admin = scope.Admin;
        var ct = TestContext.Current.CancellationToken;

        // Subjects with distinct privilege expectations.
        var nobody = await scope.RegisterUserAsync($"matrix-nobody-{Guid.NewGuid():N}@getcode.test", ct);
        var pricer = await scope.RegisterUserAsync($"matrix-pricer-{Guid.NewGuid():N}@getcode.test", ct);
        var treasurer = await scope.RegisterUserAsync($"matrix-treasurer-{Guid.NewGuid():N}@getcode.test", ct);
        var superuser = await scope.RegisterUserAsync($"matrix-super-{Guid.NewGuid():N}@getcode.test", ct);

        // Role definitions.
        await admin.CreateRoleAsync(new CreateRoleCommand("pricing-manager", "Pricing Manager"), ct);
        await admin.CreateRoleAsync(new CreateRoleCommand("treasurer", "Treasurer"), ct);
        await admin.CreateRoleAsync(new CreateRoleCommand("platform-admin", "Platform Admin", IsSystemRole: true), ct);

        // Permission bundles.
        await admin.ChangePermissionAsync(new ChangeRolePermissionsCommand("pricing-manager", PermissionCatalog.PricingManage, Grant: true), ct);
        await admin.ChangePermissionAsync(new ChangeRolePermissionsCommand("treasurer", PermissionCatalog.WalletAdjust, Grant: true), ct);
        foreach (var permission in PermissionCatalog.All)
        {
            await admin.ChangePermissionAsync(new ChangeRolePermissionsCommand("platform-admin", permission, Grant: true), ct);
        }

        // Assignments.
        await admin.SetUserRoleAsync(new AssignUserRoleCommand(pricer, "pricing-manager", Assign: true), ct);
        await admin.SetUserRoleAsync(new AssignUserRoleCommand(treasurer, "treasurer", Assign: true), ct);
        await admin.SetUserRoleAsync(new AssignUserRoleCommand(superuser, "platform-admin", Assign: true), ct);

        // Matrix: subject x permission expectation.
        await AssertMatrix(scope, nobody, expected: []);
        await AssertMatrix(scope, pricer, expected: [PermissionCatalog.PricingManage]);
        await AssertMatrix(scope, treasurer, expected: [PermissionCatalog.WalletAdjust]);
        await AssertMatrix(scope, superuser, expected: [.. PermissionCatalog.All]);

        // Revocation propagates immediately.
        await admin.ChangePermissionAsync(new ChangeRolePermissionsCommand("treasurer", PermissionCatalog.WalletAdjust, Grant: false), ct);
        await AssertMatrix(scope, treasurer, expected: []);

        // Unassignment is idempotent and audited.
        await admin.SetUserRoleAsync(new AssignUserRoleCommand(pricer, "pricing-manager", Assign: false), ct);
        await admin.SetUserRoleAsync(new AssignUserRoleCommand(pricer, "pricing-manager", Assign: false), ct); // no-op
        await AssertMatrix(scope, pricer, expected: []);

        // Unknown users cannot be assigned; unknown roles are rejected.
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => admin.SetUserRoleAsync(new AssignUserRoleCommand($"ghost-{Guid.NewGuid():N}@getcode.test", "pricing-manager", Assign: true), ct));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => admin.SetUserRoleAsync(new AssignUserRoleCommand(pricer, "no-such-role", Assign: true), ct));

        // Duplicate role creation is rejected.
        await Assert.ThrowsAsync<InvalidOperationException>(() => admin.CreateRoleAsync(new CreateRoleCommand("treasurer", "Again"), ct));

        // Every privilege change was audited through the outbox.
        var auditTypes = await scope.OutboxEventTypesAsync(ct);
        Assert.Contains(auditTypes, t => t.StartsWith("authz.role.created"));
        Assert.Contains(auditTypes, t => t.StartsWith("authz.role.permissions_changed"));
        Assert.Contains(auditTypes, t => t.StartsWith("authz.user.role_changed"));
    }

    private static async Task AssertMatrix(AuthorizationScope scope, string email, IReadOnlyCollection<string> expected)
    {
        var effective = await scope.Admin.GetEffectivePermissionsAsync(email, TestContext.Current.CancellationToken);
        foreach (var permission in expected)
        {
            Assert.Contains(permission, effective);
        }

        foreach (var permission in PermissionCatalog.All)
        {
            if (!expected.Contains(permission))
            {
                Assert.DoesNotContain(permission, effective);
            }
        }
    }

    /// <summary>Owns factory + scope lifetime and resets authorization tables on dispose.</summary>
    private sealed class AuthorizationScope : IAsyncDisposable
    {
        private readonly GetCodeApiFactory _factory;
        private readonly IServiceScope _serviceScope;

        public AuthorizationScope(DatabaseFixture database)
        {
            _factory = new GetCodeApiFactory(database);
            _serviceScope = _factory.Services.CreateScope();
            Admin = _serviceScope.ServiceProvider.GetRequiredService<AuthorizationAdminService>();
            Identity = _serviceScope.ServiceProvider.GetRequiredService<IdentityService>();
        }

        public AuthorizationAdminService Admin { get; }
        public IdentityService Identity { get; }

        public async Task<string> RegisterUserAsync(string email, CancellationToken cancellationToken)
        {
            // High-entropy random password; retry on the rare sequence-detection false positive.
            for (var attempt = 0; ; attempt++)
            {
                var password = $"Zx7#{Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(9))}!qQ";
                try
                {
                    var result = await Identity.RegisterAsync(
                        new RegisterUserCommand(email, password, CorrelationId: null),
                        cancellationToken);
                    return result.NormalizedEmail;
                }
                catch (IdentityRuleViolationException ex) when (
                    attempt < 4 && ex.Violations.Contains("password_too_predictable"))
                {
                    // regenerate and try again
                }
            }
        }

        public async Task<IReadOnlyList<string>> OutboxEventTypesAsync(CancellationToken cancellationToken)
        {
            var context = _serviceScope.ServiceProvider.GetRequiredService<Persistence.GetCodeDbContext>();
            return await context.OutboxMessages.Select(m => m.Type).ToListAsync(cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            var context = _serviceScope.ServiceProvider.GetRequiredService<Persistence.GetCodeDbContext>();
            await context.UserRoles.ExecuteDeleteAsync(CancellationToken.None);
            await context.Roles.ExecuteDeleteAsync(CancellationToken.None);
            await context.OutboxMessages.Where(m => m.Type.StartsWith("authz.")).ExecuteDeleteAsync(CancellationToken.None);
            _serviceScope.Dispose();
            await _factory.DisposeAsync();
        }
    }
}
