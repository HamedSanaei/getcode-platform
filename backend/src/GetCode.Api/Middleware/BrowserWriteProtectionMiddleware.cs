using GetCode.Application.SiteHosts;

namespace GetCode.Api.Middleware;

/// <summary>
/// M02-003 CSRF + origin policy for state-changing browser requests.
///
/// Layers (all must pass for non-safe methods under /api/auth and other
/// browser-facing write surfaces; machine-to-machine callback paths are
/// excluded by prefix because their authenticity comes from gateway
/// signatures, not browser origin):
/// 1. Origin/Referer (when the browser supplies one) must match the current
///    site's public base URL — cross-site form posts fail before touching
///    application logic.
/// 2. ASP.NET Core antiforgery double-submit: a __Host-prefixed, Strict,
///    JS-readable cookie paired with the X-XSRF-TOKEN header. SameSite=Lax on
///    the session cookie is defense in depth, not the primary mechanism.
/// </summary>
internal sealed class BrowserWriteProtectionMiddleware(RequestDelegate next)
{
    // Paths that are exempt because they are not browser-driven writes
    // (provider/gateway callbacks verify authenticity via signatures).
    private static readonly string[] CallbackPrefixes = ["/api/callbacks", "/api/webhooks"];

    public async Task InvokeAsync(
        HttpContext context,
        ICurrentSite currentSite,
        Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery)
    {
        var method = context.Request.Method;
        var isSafeMethod = HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method);
        var path = context.Request.Path.Value ?? string.Empty;
        var isCallbackPath = CallbackPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        if (!isSafeMethod && path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) && !isCallbackPath)
        {
            if (!OriginMatchesSite(context, currentSite.Site))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            try
            {
                await antiforgery.ValidateRequestAsync(context);
            }
            catch (Microsoft.AspNetCore.Antiforgery.AntiforgeryValidationException)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
        }

        await next(context);
    }

    private static bool OriginMatchesSite(HttpContext context, SiteDescriptor site)
    {
        var origin = context.Request.Headers.Origin.ToString();
        if (string.IsNullOrEmpty(origin))
        {
            // Non-browser clients (curl, server-to-server) may omit Origin;
            // the antiforgery token then carries the burden.
            return true;
        }

        return Uri.TryCreate(origin, UriKind.Absolute, out var originUri)
            && string.Equals(originUri.Authority, site.PublicBaseUri.Authority, StringComparison.OrdinalIgnoreCase)
            && string.Equals(originUri.Scheme, site.PublicBaseUri.Scheme, StringComparison.OrdinalIgnoreCase);
    }
}
