using GetCode.Application.Identity;
using GetCode.Application.SiteHosts;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace GetCode.Api.Endpoints;

/// <summary>
/// M02-002: session cookie strategy over HTTP.
///
/// Policy (tested in SessionIntegrationTests):
/// - Cookie names are per-site and use the __Host- prefix, which browsers
///   refuse to accept unless Secure is set, the Path is "/", and — critically —
///   no Domain attribute is present. A cookie therefore can never be scoped to
///   a parent domain shared by two unrelated sites.
/// - primary  → __Host-gc_session
/// - pluspremium → __Host-vpp_session
/// - SameSite=Lax (strict CSRF policy lands with M02-003), HttpOnly, Secure.
/// - Absolute lifetime of 7 days (SessionService.AbsoluteLifetime); each login
///   issues a fresh token; logout revokes server-side and clears the cookie.
/// - The site key comes from resolved site context, not from client input, and
///   is re-checked server-side on every validation (SiteMismatch defense).
/// </summary>
internal static class AuthEndpoints
{
    public static string CookieNameFor(string siteKey) => siteKey switch
    {
        SessionService.PlusPremiumSiteKey => "__Host-vpp_session",
        _ => "__Host-gc_session",
    };

    public static IEndpointConventionBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", async (
            LoginRequest request,
            IdentityService identity,
            SessionService sessions,
            GetCode.Application.SiteHosts.ICurrentSite currentSite,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var result = await identity.AuthenticateAsync(
                new AuthenticateCommand(request.Email, request.Password), cancellationToken);
            if (result is not AuthenticateResult.Success success)
            {
                // Uniform failure regardless of reason; lockout details stay out of the response.
                return Results.Json(new { error = "invalid_credentials" }, statusCode: StatusCodes.Status401Unauthorized);
            }

            var issued = await sessions.IssueAsync(success.UserId, currentSite.Site.Key, cancellationToken);
            var expiresInSeconds = (int)Math.Max(0, (issued.ExpiresAtUtc - DateTimeOffset.UtcNow).TotalSeconds);
            http.Response.Cookies.Append(
                CookieNameFor(issued.SiteKey),
                issued.Token,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Path = "/",
                    MaxAge = TimeSpan.FromSeconds(expiresInSeconds),
                });
            return Results.Ok(new SessionResponse(issued.UserId));
        })
        .Accepts<LoginRequest>("application/json")
        .Produces<SessionResponse>()
        .Produces(StatusCodes.Status401Unauthorized)
        .WithSummary("Authenticates a user and issues a host-scoped session cookie");

        group.MapGet("/session", async (
            SessionService sessions,
            GetCode.Application.SiteHosts.ICurrentSite currentSite,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var token = http.Request.Cookies[CookieNameFor(currentSite.Site.Key)];
            var validation = await sessions.ValidateAsync(token, currentSite.Site.Key, cancellationToken);
            if (validation is not SessionValidationResult.Success success)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new SessionResponse(success.UserId));
        })
        .Produces<SessionResponse>()
        .Produces(StatusCodes.Status401Unauthorized)
        .WithSummary("Returns the authenticated user for the current host's session cookie");

        group.MapPost("/logout", async (
            SessionService sessions,
            GetCode.Application.SiteHosts.ICurrentSite currentSite,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var cookieName = CookieNameFor(currentSite.Site.Key);
            var token = http.Request.Cookies[cookieName];
            if (!string.IsNullOrWhiteSpace(token))
            {
                await sessions.RevokeAsync(token, cancellationToken);
            }

            // Idempotent: clearing the cookie succeeds even without a valid session.
            http.Response.Cookies.Delete(cookieName, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
            });
            return Results.NoContent();
        })
        .Produces(StatusCodes.Status204NoContent)
        .WithSummary("Revokes the current host's session and clears its cookie");

        group.MapPost("/session/rotate", async (
            SessionService sessions,
            GetCode.Application.SiteHosts.ICurrentSite currentSite,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var cookieName = CookieNameFor(currentSite.Site.Key);
            var token = http.Request.Cookies[cookieName];
            if (string.IsNullOrWhiteSpace(token))
            {
                return Results.Unauthorized();
            }

            var rotated = await sessions.RotateAsync(token, currentSite.Site.Key, cancellationToken);
            if (rotated is null)
            {
                return Results.Unauthorized();
            }

            var expiresInSeconds = (int)Math.Max(0, (rotated.ExpiresAtUtc - DateTimeOffset.UtcNow).TotalSeconds);
            http.Response.Cookies.Append(cookieName, rotated.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                MaxAge = TimeSpan.FromSeconds(expiresInSeconds),
            });
            return Results.Ok(new SessionResponse(rotated.UserId));
        })
        .Produces<SessionResponse>()
        .Produces(StatusCodes.Status401Unauthorized)
        .WithSummary("Rotates the current session: old token revoked, fresh cookie issued");

        // M02-003: hands the SPA an antiforgery token pair (cookie + body value
        // for the X-XSRF-TOKEN header). Safe method — no validation here.
        group.MapGet("/csrf", (IAntiforgery antiforgery, HttpContext http) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(http);
            return Results.Ok(new CsrfTokenResponse(tokens.RequestToken!));
        })
        .Produces<CsrfTokenResponse>()
        .WithSummary("Issues the CSRF token pair used by state-changing browser requests");

        // M02-003: trusted redirect resolution. Browser-supplied return URLs are
        // resolved against the Site Context allow-list; foreign targets collapse
        // to the current site's base URL.
        group.MapGet("/redirect-target", (
            string? returnUrl,
            TrustedRedirectResolver resolver,
            GetCode.Application.SiteHosts.ICurrentSite currentSite) =>
        {
            var resolved = resolver.ResolveReturnUrl(returnUrl, currentSite.Site);
            return Results.Ok(new RedirectTargetResponse(resolved));
        })
        .Produces<RedirectTargetResponse>()
        .WithSummary("Resolves a return URL to a trusted target on the configured sites");

        return group;
    }
}

public sealed record LoginRequest(string Email, string Password);

public sealed record SessionResponse(Guid UserId);

public sealed record CsrfTokenResponse(string RequestToken);

public sealed record RedirectTargetResponse(string ResolvedUrl);
