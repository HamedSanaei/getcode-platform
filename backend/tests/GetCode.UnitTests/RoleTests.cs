using GetCode.Domain.Authorization;

namespace GetCode.UnitTests;

public sealed class RoleTests
{
    [Fact]
    public void Create_normalizes_key_and_rejects_bad_keys()
    {
        var role = Role.Create("Support-Agent ", "Support Agent", DateTimeOffset.UtcNow);
        Assert.Equal("support-agent", role.Key);

        Assert.Throws<ArgumentException>(() => Role.Create("Bad_Key", "x", DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => Role.Create("", "x", DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => Role.Create("ok", " ", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Grant_rejects_unknown_permissions_deny_by_default_definitions()
    {
        var role = Role.Create("ops", "Operations", DateTimeOffset.UtcNow);

        // Only registered canonical scopes may ever enter a role definition.
        Assert.Throws<UnauthorizedAccessException>(() => role.Grant("*", DateTimeOffset.UtcNow));
        Assert.Throws<UnauthorizedAccessException>(() => role.Grant("orders.delete_everything", DateTimeOffset.UtcNow));
        Assert.Throws<UnauthorizedAccessException>(() => role.Revoke("wallet.adjustments", DateTimeOffset.UtcNow));

        // Revoking a known but ungranted permission is an idempotent no-op.
        role.Revoke(PermissionCatalog.WalletAdjust, DateTimeOffset.UtcNow);
        Assert.False(role.Has(PermissionCatalog.WalletAdjust));
    }

    [Fact]
    public void Grant_then_revoke_emits_change_events_once_per_state_change()
    {
        var now = DateTimeOffset.UtcNow;
        var role = Role.Create("pricing-manager", "Pricing Manager", now);
        role.ClearDomainEvents();

        role.Grant(PermissionCatalog.PricingManage, now);
        role.Grant(PermissionCatalog.PricingManage, now); // idempotent: no second event
        Assert.Single(role.DomainEvents);
        Assert.True(role.Has(PermissionCatalog.PricingManage));

        role.Revoke(PermissionCatalog.PricingManage, now);
        Assert.Equal(2, role.DomainEvents.Count);
        Assert.False(role.Has(PermissionCatalog.PricingManage));

        var changed = Assert.IsType<RolePermissionsChanged>(role.DomainEvents.Last());
        Assert.Empty(changed.Permissions);
    }

    [Fact]
    public void Permission_catalog_contains_contract_scopes()
    {
        foreach (var expected in new[] { "orders.read", "orders.refund", "pricing.manage", "providers.manage", "wallet.adjust" })
        {
            Assert.Contains(expected, PermissionCatalog.All);
        }
    }
}
