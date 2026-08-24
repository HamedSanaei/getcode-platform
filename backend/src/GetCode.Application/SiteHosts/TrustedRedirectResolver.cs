namespace GetCode.Application.SiteHosts;

/// <summary>
/// M02-003 trusted redirect policy. Browser-supplied return URLs are never
/// echoed or redirected to verbatim: the candidate must either be a
/// same-site relative path (single leading slash) or an exact match of a
/// configured site's public base URL (optionally plus a path). Anything else —
/// absolute foreign origins, scheme-relative "//host" forms, backslash tricks —
/// resolves to the current site's base URL. This makes open redirects and
/// cross-domain redirect abuse impossible by construction.
/// </summary>
public sealed class TrustedRedirectResolver(ISiteCatalog sites)
{
    /// <summary>Resolves a browser-supplied return target to an allow-listed absolute URL.</summary>
    public string ResolveReturnUrl(string? returnUrl, SiteDescriptor currentSite)
    {
        var fallback = TrimTrailingSlash(currentSite.PublicBaseUri.ToString());

        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return fallback;
        }

        var candidate = returnUrl.Trim();

        // Relative path on the current site: exactly one leading slash, no
        // scheme-relative "//", no backslashes, no control characters.
        if (candidate.StartsWith('/'))
        {
            if (!candidate.StartsWith("//")
                && !candidate.Contains('\\')
                && !candidate.Any(char.IsControl)
                && Uri.IsWellFormedUriString($"https://placeholder.example{candidate}", UriKind.Absolute))
            {
                return fallback + candidate;
            }

            return fallback;
        }

        // Absolute URL: allowed only when it is one of the configured sites.
        if (Uri.TryCreate(candidate, UriKind.Absolute, out var uri)
            && string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var site in sites.Sites)
            {
                var allowedBase = site.PublicBaseUri;
                if (string.Equals(uri.Authority, allowedBase.Authority, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(uri.Scheme, allowedBase.Scheme, StringComparison.OrdinalIgnoreCase))
                {
                    return uri.ToString();
                }
            }
        }

        return fallback;
    }

    private static string TrimTrailingSlash(string url) =>
        url.EndsWith('/') ? url[..^1] : url;
}
