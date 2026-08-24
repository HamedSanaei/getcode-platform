using GetCode.Domain.Authorization;

namespace GetCode.Application.Authorization;

public interface IRoleRepository
{
    Task<Role?> FindByKeyAsync(string key, CancellationToken cancellationToken);
    Task<Role?> FindByIdAsync(Guid roleId, CancellationToken cancellationToken);
    void Add(Role role);
}

/// <summary>User-to-role assignment storage; a user with no assignments has no permissions.</summary>
public interface IUserRoleRepository
{
    /// <summary>True when the assignment already exists (idempotent grant).</summary>
    Task<bool> IsAssignedAsync(Guid userId, Guid roleId, CancellationToken cancellationToken);
    /// <summary>Role ids assigned to the user; effective permissions are their union.</summary>
    Task<IReadOnlyList<Guid>> ListRoleIdsForUserAsync(Guid userId, CancellationToken cancellationToken);
    void Assign(Guid userId, Guid roleId);
    Task UnassignAsync(Guid userId, Guid roleId, CancellationToken cancellationToken);
}

/// <summary>
/// Effective-permission resolution. Deny-by-default: the union of assigned
/// roles' permissions, empty for users without roles.
/// </summary>
public interface IAuthorizationService
{
    Task<IReadOnlySet<string>> GetEffectivePermissionsAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken);
}
