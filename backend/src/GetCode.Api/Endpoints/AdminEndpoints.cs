using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GetCode.Api.Endpoints;

/// <summary>
/// M09-001: admin API surface. Every endpoint in this group requires the
/// <c>admin.access</c> capability, enforced server-side by the permission
/// policy (session authentication + effective-permission check). Hidden
/// navigation in the SPA is a UX affordance only — these policies are the
/// security boundary.
/// </summary>
internal static class AdminEndpoints
{
    public static IEndpointConventionBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Admin")
            .RequireAuthorization("admin.access");

        group.MapGet("/overview", (TimeProvider clock) =>
            Results.Ok(new AdminOverviewResponse(clock.GetUtcNow())))
            .Produces<AdminOverviewResponse>()
            .WithSummary("Shell bootstrap payload for the admin overview");

        return group;
    }

    public sealed record AdminOverviewResponse(DateTimeOffset ServerTimeUtc);
}
