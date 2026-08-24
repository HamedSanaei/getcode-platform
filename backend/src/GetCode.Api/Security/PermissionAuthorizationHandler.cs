using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using AppAuthZ = GetCode.Application.Authorization;

namespace GetCode.Api.Security;

/// <summary>Authorization requirement for a single canonical capability.</summary>
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

/// <summary>
/// M09-001: evaluates a canonical permission against the effective permissions
/// of the authenticated user (deny-by-default). Admin endpoints declare
/// policies; they never compare role names inline — the backend remains the
/// only authorization boundary.
/// </summary>
public sealed class PermissionAuthorizationHandler(AppAuthZ.IAuthorizationService authorization)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return; // not authenticated as a session principal → stays denied
        }

        if (await authorization.HasPermissionAsync(userId, requirement.Permission, CancellationToken.None))
        {
            context.Succeed(requirement);
        }
    }
}
