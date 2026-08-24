namespace GetCode.Api.Middleware;

/// <summary>
/// M02-003 CORS policy. ADR-006: the browser path is same-origin /api/*, so
/// cross-origin credentialed access is allow-list-only and empty by default —
/// no configuration means zero cross-origin access, never a wildcard.
/// Origins are read from Cors:AllowedOrigins (array or comma-separated).
/// </summary>
public sealed class BrowserCorsOptions
{
    public const string SectionName = "Cors";
    public const string PolicyName = "browser";

    public string[] AllowedOrigins { get; init; } = [];

    /// <summary>Parses array-or-comma-separated configuration into distinct origins.</summary>
    public static string[] ParseOrigins(string? raw) =>
        (raw ?? string.Empty)
            .Split([',', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(o => o.TrimEnd('/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
