using GetCode.Application.Catalog;
using GetCode.Domain.Authorization;
using GetCode.Domain.Identity;
using GetCode.Application.Identity;
using System.Text.Json;

namespace GetCode.Application.Authorization;

public sealed record CreateRoleCommand(string Key, string DisplayName, bool IsSystemRole = false, string? CorrelationId = null);

public sealed record ChangeRolePermissionsCommand(string RoleKey, string Permission, bool Grant, string? CorrelationId = null);

public sealed record AssignUserRoleCommand(string UserEmail, string RoleKey, bool Assign, string? CorrelationId = null);

/// <summary>
/// Authorization administration. Every privilege change (role creation,
/// permission grant/revoke, user assignment) is mirrored into the
/// transactional outbox as an audit event — privilege changes are never silent.
/// </summary>
public sealed class AuthorizationAdminService(
    IRoleRepository roles,
    IUserRoleRepository userRoles,
    IUserRepository users,
    IAuthorizationService authorization,
    IOutboxCollector outbox,
    ICatalogUnitOfWork unitOfWork)
{
    public async Task<Guid> CreateRoleAsync(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        var key = command.Key.Trim().ToLowerInvariant();
        if (await roles.FindByKeyAsync(key, cancellationToken) is not null)
        {
            throw new InvalidOperationException($"Role '{key}' already exists.");
        }

        var role = Role.Create(key, command.DisplayName, DateTimeOffset.UtcNow, command.IsSystemRole);
        roles.Add(role);

        Collect("authz.role.created", new { roleKey = role.Key, isSystemRole = role.IsSystemRole }, command.CorrelationId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return role.Id;
    }

    /// <summary>Grants or revokes a single permission on a role.</summary>
    public async Task ChangePermissionAsync(ChangeRolePermissionsCommand command, CancellationToken cancellationToken)
    {
        var role = await RequireRoleAsync(command.RoleKey, cancellationToken);

        if (command.Grant)
        {
            role.Grant(command.Permission, DateTimeOffset.UtcNow);
        }
        else
        {
            role.Revoke(command.Permission, DateTimeOffset.UtcNow);
        }

        Collect("authz.role.permissions_changed", new
        {
            roleKey = role.Key,
            permission = command.Permission,
            granted = command.Grant,
            permissions = role.Permissions.ToArray(),
        }, command.CorrelationId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetUserRoleAsync(AssignUserRoleCommand command, CancellationToken cancellationToken)
    {
        var normalizedEmail = EmailNormalizer.Normalize(command.UserEmail);
        var user = await users.FindByNormalizedEmailAsync(normalizedEmail, cancellationToken)
            ?? throw new InvalidOperationException($"User '{normalizedEmail}' was not found.");
        var role = await RequireRoleAsync(command.RoleKey, cancellationToken);

        var alreadyAssigned = await userRoles.IsAssignedAsync(user.Id, role.Id, cancellationToken);
        if (!alreadyAssigned && command.Assign)
        {
            userRoles.Assign(user.Id, role.Id);
            await AuditAssignmentAsync(user.Id, role.Key, assigned: true, command.CorrelationId, cancellationToken);
        }
        else if (alreadyAssigned && !command.Assign)
        {
            await userRoles.UnassignAsync(user.Id, role.Id, cancellationToken);
            await AuditAssignmentAsync(user.Id, role.Key, assigned: false, command.CorrelationId, cancellationToken);
        }

        // Idempotent no-ops are not audited twice; state change alone emits events.
    }

    private async Task AuditAssignmentAsync(Guid userId, string roleKey, bool assigned, string? correlationId, CancellationToken cancellationToken)
    {
        Collect("authz.user.role_changed", new
        {
            userId,
            roleKey,
            assigned,
        }, correlationId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlySet<string>> GetEffectivePermissionsAsync(string userEmail, CancellationToken cancellationToken)
    {
        var normalizedEmail = EmailNormalizer.Normalize(userEmail);
        var user = await users.FindByNormalizedEmailAsync(normalizedEmail, cancellationToken)
            ?? throw new InvalidOperationException($"User '{normalizedEmail}' was not found.");

        return await authorization.GetEffectivePermissionsAsync(user.Id, cancellationToken);
    }

    private async Task<Role> RequireRoleAsync(string roleKey, CancellationToken cancellationToken) =>
        await roles.FindByKeyAsync(roleKey.Trim().ToLowerInvariant(), cancellationToken)
        ?? throw new InvalidOperationException($"Role '{roleKey}' was not found.");

    private void Collect(string type, object payload, string? correlationId) =>
        outbox.Collect(type, JsonSerializer.Serialize(payload, CatalogAdminService.PayloadOptions), correlationId);
}

/// <summary>
/// Deny-by-default effective permission resolution over assigned roles.
/// </summary>
public sealed class EffectiveAuthorizationService(
    IRoleRepository roles,
    IUserRoleRepository userRoles) : IAuthorizationService
{
    public async Task<IReadOnlySet<string>> GetEffectivePermissionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var roleIds = await userRoles.ListRoleIdsForUserAsync(userId, cancellationToken);
        var effective = new HashSet<string>(StringComparer.Ordinal);
        foreach (var roleId in roleIds)
        {
            var role = await roles.FindByIdAsync(roleId, cancellationToken);
            if (role is not null)
            {
                effective.UnionWith(role.Permissions);
            }
        }

        return effective;
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken)
    {
        if (!PermissionCatalog.All.Contains(permission))
        {
            return false; // unknown permission: deny
        }

        var permissions = await GetEffectivePermissionsAsync(userId, cancellationToken);
        return permissions.Contains(permission);
    }
}
