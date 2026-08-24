using GetCode.Application.Catalog;
using GetCode.Application.Common;
using GetCode.Contracts.Catalog;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GetCode.Api.Endpoints;

/// <summary>
/// M03-004: public catalog read models. Anonymous, same-origin /api/* reads
/// (ADR-006). Only enabled/offered entries are exposed; provider routing data
/// never appears on this surface. Responses are deterministic and paged, so a
/// cache can sit in front without Redis becoming truth.
/// </summary>
internal static class CatalogEndpoints
{
    private const string DefaultCulture = "en";

    public static IEndpointConventionBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/catalog").WithTags("Catalog").AllowAnonymous();

        group.MapGet("/countries", async (
                string? culture, int? page, int? pageSize,
                CatalogQueryService queries, CancellationToken cancellationToken) =>
            {
                var result = await queries.ListCountriesPagedAsync(
                    culture ?? DefaultCulture, PageRequest.Create(page ?? 1, pageSize ?? 50), cancellationToken);
                return Results.Ok(new CatalogPageResponse<CountryResponse>(
                    [.. result.Items.Select(c => new CountryResponse(c.StableKey, c.DisplayName, c.DisplayOrder))],
                    result.Page, result.PageSize, result.TotalCount, result.TotalPages));
            })
            .Produces<CatalogPageResponse<CountryResponse>>()
            .WithSummary("Lists enabled countries with display names in the requested culture");

        group.MapGet("/services", async (
                string? culture, int? page, int? pageSize,
                CatalogQueryService queries, CancellationToken cancellationToken) =>
            {
                var result = await queries.ListServicesPagedAsync(
                    culture ?? DefaultCulture, PageRequest.Create(page ?? 1, pageSize ?? 50), cancellationToken);
                return Results.Ok(new CatalogPageResponse<ServiceResponse>(
                    [.. result.Items.Select(s => new ServiceResponse(s.StableKey, s.DisplayName, s.DisplayOrder))],
                    result.Page, result.PageSize, result.TotalCount, result.TotalPages));
            })
            .Produces<CatalogPageResponse<ServiceResponse>>()
            .WithSummary("Lists enabled services with display names in the requested culture");

        group.MapGet("/offers", async (
                string? culture, int? page, int? pageSize,
                ProductCatalogQueryService queries, CancellationToken cancellationToken) =>
            {
                var result = await queries.ListOfferedSkusPagedAsync(
                    culture ?? DefaultCulture, PageRequest.Create(page ?? 1, pageSize ?? 25), cancellationToken);
                return Results.Ok(new CatalogPageResponse<OfferResponse>(
                    [.. result.Items.Select(v => new OfferResponse(
                        v.StableKey, v.CountryCode, v.ServiceSlug, v.CountryDisplayName, v.ServiceDisplayName, v.ProductType.ToString()))],
                    result.Page, result.PageSize, result.TotalCount, result.TotalPages));
            })
            .Produces<CatalogPageResponse<OfferResponse>>()
            .WithSummary("Lists currently offered virtual-number products (canonical view, no provider data)");

        return group;
    }
}
