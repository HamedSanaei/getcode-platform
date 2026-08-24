using GetCode.Application.Catalog;
using AppProviders = GetCode.Application.Providers;
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

        // M04-003: latest provider health/balance observations (supplier
        // telemetry — never customer wallet truth).
        group.MapGet("/providers/health", (AppProviders.ProviderHealthService health) =>
            {
                var snapshots = health.LatestSnapshots;
                return Results.Ok(new ProviderHealthResponse(
                    [.. snapshots.Select(s => new ProviderHealthItem(
                        s.ProviderKey, s.Outcome.ToString(), s.BalanceAmount, s.SafeErrorToken, s.ConsecutiveFailures, s.ObservedAtUtc))]));
            })
            .Produces<ProviderHealthResponse>()
            .WithSummary("Latest normalized health/balance observation per provider");

        // M09-003: catalog/provider mapping management. All mutations are
        // validated against the canonical catalog and audited via the
        // transactional outbox by ProviderAdminService.
        var providersGroup = group.MapGroup("/providers");

        providersGroup.MapGet("/", async (AppProviders.ProviderAdminService service, CancellationToken ct) =>
            {
                var list = await service.ListForManagementAsync(ct);
                return Results.Ok(list.Select(ToResponse).ToArray());
            })
            .Produces<ProviderManagementResponse[]>()
            .WithSummary("Lists registered providers with their canonical mappings");

        providersGroup.MapPost("/register", async (
            RegisterProviderRequest request,
            AppProviders.ProviderAdminService service, HttpContext http, CancellationToken ct) =>
        {
            try
            {
                await service.RegisterAsync(new AppProviders.RegisterProviderCommand(
                    request.ProviderKey, request.DisplayName,
                    request.SupportsActivation, request.SupportsRental,
                    http.TraceIdentifier), ct);
                return Results.Ok(new { status = "registered" });
            }
            catch (ArgumentException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["providerKey"] = [ex.Message] });
            }
        })
        .Accepts<RegisterProviderRequest>("application/json")
        .WithSummary("Registers a provider (idempotent on providerKey)");

        providersGroup.MapPost("/set-enabled", async (
            SetProviderEnabledRequest request,
            AppProviders.ProviderAdminService service, HttpContext http, CancellationToken ct) =>
        {
            try
            {
                await service.SetEnabledAsync(new AppProviders.SetProviderEnabledCommand(
                    request.ProviderKey, request.Enabled, http.TraceIdentifier), ct);
                return Results.Ok(new { status = "updated" });
            }
            catch (GetCode.Application.Catalog.CatalogEntryNotFoundException)
            {
                return Results.NotFound(new { error = "unknown provider" });
            }
        })
        .WithSummary("Enables or disables a provider");

        var mappingsGroup = group.MapGroup("/mappings");

        mappingsGroup.MapPost("/preview", async (
            BindMappingRequest request,
            AppProviders.ProviderAdminService service, CancellationToken ct) =>
        {
            if (!Enum.TryParse<Domain.Providers.MappingKind>(request.Kind, ignoreCase: true, out var kind))
            {
                return Results.BadRequest(new { error = "kind must be 'Country' or 'Service'" });
            }

            var preview = await service.PreviewBindAsync(
                new AppProviders.BindCanonicalMappingCommand(request.ProviderKey, kind, request.ExternalCode, request.CanonicalStableKey), ct);
            return Results.Ok(preview);
        })
        .WithSummary("Dry-run: resolves the canonical target without mutating anything");

        mappingsGroup.MapPost("/bind", async (
            BindMappingRequest request,
            AppProviders.ProviderAdminService service, HttpContext http, CancellationToken ct) =>
        {
            if (!Enum.TryParse<Domain.Providers.MappingKind>(request.Kind, ignoreCase: true, out var kind))
            {
                return Results.BadRequest(new { error = "kind must be 'Country' or 'Service'" });
            }

            try
            {
                var id = await service.BindMappingAsync(
                    new AppProviders.BindCanonicalMappingCommand(
                        request.ProviderKey, kind, request.ExternalCode, request.CanonicalStableKey, http.TraceIdentifier), ct);
                return Results.Ok(new { mappingId = id });
            }
            catch (GetCode.Application.Catalog.CatalogEntryNotFoundException ex)
            {
                // Invalid targets never touch the catalog: rejected before any mutation.
                return Results.NotFound(new { error = $"unknown {ex.Kind} '{ex.StableKey}'" });
            }
        })
        .WithSummary("Binds or re-binds a provider external code to a canonical entry");

        return group;
    }

    private static ProviderManagementResponse ToResponse(AppProviders.ProviderManagementView view) => new(
        view.ProviderKey, view.DisplayName, view.IsEnabled, view.SupportsActivation, view.SupportsRental,
        [.. view.Mappings.Select(m => new MappingResponse(m.Kind.ToString(), m.ExternalCode, m.CanonicalStableKey))]);

    public sealed record AdminOverviewResponse(DateTimeOffset ServerTimeUtc);

    public sealed record RegisterProviderRequest(string ProviderKey, string DisplayName, bool SupportsActivation = true, bool SupportsRental = false);

    public sealed record SetProviderEnabledRequest(string ProviderKey, bool Enabled);

    public sealed record BindMappingRequest(string ProviderKey, string Kind, string ExternalCode, string CanonicalStableKey);

    public sealed record MappingResponse(string Kind, string ExternalCode, string CanonicalStableKey);

    public sealed record ProviderManagementResponse(string ProviderKey, string DisplayName, bool IsEnabled, bool SupportsActivation, bool SupportsRental, IReadOnlyList<MappingResponse> Mappings);

    public sealed record ProviderHealthItem(string ProviderKey, string Outcome, decimal? BalanceAmount, string? SafeErrorToken, int ConsecutiveFailures, DateTimeOffset ObservedAtUtc);

    public sealed record ProviderHealthResponse(IReadOnlyList<ProviderHealthItem> Providers);
}
