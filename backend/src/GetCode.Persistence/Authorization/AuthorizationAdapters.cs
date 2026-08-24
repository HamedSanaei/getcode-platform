using GetCode.Application.Authorization;
using GetCode.Domain.Authorization;
using GetCode.Persistence.Catalog;
using Microsoft.EntityFrameworkCore;

namespace GetCode.Persistence.Authorization;

internal sealed class RoleRepository(GetCodeDbContext context) : IRoleRepository
{
    public Task<Role?> FindByKeyAsync(string key, CancellationToken cancellationToken) =>
        context.Roles.FirstOrDefaultAsync(r => r.Key == key.Trim().ToLowerInvariant(), cancellationToken);

    public Task<Role?> FindByIdAsync(Guid roleId, CancellationToken cancellationToken) =>
        context.Roles.FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

    public void Add(Role role) => context.Roles.Add(role);
}

/// <summary>
/// Assignment storage keyed by (userId, roleId). Effective permissions are
/// resolved through assigned roles only — deny-by-default with no implicit grants.
/// </summary>
internal sealed class UserRoleRepository(GetCodeDbContext context) : IUserRoleRepository
{
    public Task<bool> IsAssignedAsync(Guid userId, Guid roleId, CancellationToken cancellationToken) =>
        context.UserRoles.AnyAsync(a => a.UserId == userId && a.RoleId == roleId, cancellationToken);

    public async Task<IReadOnlyList<Guid>> ListRoleIdsForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await context.UserRoles
            .Where(a => a.UserId == userId)
            .Select(a => a.RoleId)
            .ToListAsync(cancellationToken);

    public void Assign(Guid userId, Guid roleId) =>
        context.UserRoles.Add(UserRoleAssignmentRecord.Create(userId, roleId, DateTimeOffset.UtcNow));

    public async Task UnassignAsync(Guid userId, Guid roleId, CancellationToken cancellationToken)
    {
        var existing = await context.UserRoles.FirstOrDefaultAsync(a => a.UserId == userId && a.RoleId == roleId, cancellationToken);
        if (existing is not null)
        {
            context.UserRoles.Remove(existing);
        }
    }
}
