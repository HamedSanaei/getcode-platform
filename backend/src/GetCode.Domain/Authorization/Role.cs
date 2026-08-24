using GetCode.Domain.Common;

namespace GetCode.Domain.Authorization;

/// <summary>
/// Well-known authorization scopes. Keys are stable contract strings;
/// product features must reuse these instead of inventing parallel schemes.
/// </summary>
public static class PermissionCatalog
{
    public const string OrdersRead = "orders.read";
    public const string OrdersRefund = "orders.refund";
    public const string PricingManage = "pricing.manage";
    public const string ProvidersManage = "providers.manage";
    public const string WalletAdjust = "wallet.adjust";

    /// <summary>Registry of every valid permission; granting anything else is rejected.</summary>
    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        OrdersRead,
        OrdersRefund,
        PricingManage,
        ProvidersManage,
        WalletAdjust,
    };
}

/// <summary>
/// A named bundle of permissions. System roles cannot be deleted (only edited),
/// which keeps bootstrap administrators assignable.
/// </summary>
public sealed partial class Role : AggregateRoot<Guid>
{
    private Role(Guid id, string key, string displayName)
        : base(id)
    {
        Key = key;
        DisplayName = displayName;
        Permissions = [];
    }

    /// <summary>EF materialization constructor.</summary>
    private Role()
        : base(Guid.Empty)
    {
        Key = string.Empty;
        DisplayName = string.Empty;
        Permissions = [];
    }

    public string Key { get; private set; }
    public string DisplayName { get; private set; }
    public bool IsSystemRole { get; private set; }

    // Mapped as a jsonb primitive collection; membership semantics live in this class.
    public List<string> Permissions { get; private set; }

    public static Role Create(string? key, string? displayName, DateTimeOffset nowUtc, bool isSystemRole = false, Guid? id = null)
    {
        var normalizedKey = NormalizeKey(key);
        if (normalizedKey is null || !KeyPattern().IsMatch(normalizedKey))
        {
            throw new ArgumentException("Role key must be lowercase kebab-case (e.g. 'support-agent').", nameof(key));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name is required.", nameof(displayName));
        }

        var role = new Role(id ?? Guid.CreateVersion7(), normalizedKey, displayName.Trim()) { IsSystemRole = isSystemRole };
        role.Raise(new RoleRegistered(role.Id, role.Key, nowUtc));
        return role;
    }

    /// <summary>Grants a permission; must be one of the registered canonical scopes.</summary>
    public void Grant(string permission, DateTimeOffset nowUtc)
    {
        ValidatePermission(permission);
        if (!Permissions.Contains(permission))
        {
            Permissions.Add(permission);
            Raise(new RolePermissionsChanged(Key, [.. Permissions], nowUtc));
        }
    }

    public void Revoke(string permission, DateTimeOffset nowUtc)
    {
        ValidatePermission(permission);
        if (Permissions.Remove(permission))
        {
            Raise(new RolePermissionsChanged(Key, [.. Permissions], nowUtc));
        }
    }

    public bool Has(string permission) => Permissions.Contains(permission);

    [System.Text.RegularExpressions.GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial System.Text.RegularExpressions.Regex KeyPattern();

    private static string? NormalizeKey(string? key) => key?.Trim().ToLowerInvariant();

    private static void ValidatePermission(string permission)
    {
        // Unknown permission strings are rejected outright: deny-by-default extends to definitions.
        if (!PermissionCatalog.All.Contains(permission))
        {
            throw new UnauthorizedAccessException($"Unknown permission '{permission}'.");
        }
    }
}
