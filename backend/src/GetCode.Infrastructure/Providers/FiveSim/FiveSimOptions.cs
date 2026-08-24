using System.Text.Json;
using System.Text.Json.Serialization;

namespace GetCode.Infrastructure.Providers.FiveSim;

/// <summary>
/// M04-002: configuration for the 5SIM virtual-number adapter
/// (<c>Infrastructure/Providers/FiveSim</c>). The API token is a secret read
/// from configuration/secrets only — never from source, never logged.
/// </summary>
public sealed class FiveSimOptions
{
    public const string SectionName = "FiveSim";

    /// <summary>Feature switch; the adapter is registered only when true.</summary>
    public bool Enabled { get; set; }

    /// <summary>5SIM API token (secret). Never logged, never serialized.</summary>
    public string ApiToken { get; set; } = string.Empty;

    /// <summary>Official API origin (HTTPS). Override only for stubbed tests.</summary>
    public string BaseUrl { get; set; } = "https://5sim.net";

    /// <summary>Explicit outbound timeout for every provider HTTP call.</summary>
    public int TimeoutSeconds { get; set; } = 15;

    /// <summary>
    /// Canonical country key → 5SIM country name (their protocol uses lowercase
    /// names such as "germany"). This vendor mapping lives here — not in
    /// Domain/Application.
    /// </summary>
    public Dictionary<string, string> CountryMap { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["RU"] = "russia",
        ["DE"] = "germany",
        ["GB"] = "england",
        ["US"] = "usa",
        ["IR"] = "iran",
        ["NL"] = "netherlands",
        ["FR"] = "france",
    };

    /// <summary>Operator used when an offer does not pin one ("any" = no filter).</summary>
    public string DefaultOperator { get; set; } = "any";
}

/// <summary>
/// Internal wire models of the current 5SIM protocol (New API). These types are
/// `internal` and never escape Infrastructure; the adapter maps them onto the
/// canonical provider contracts.
/// </summary>
internal static class FiveSimWire
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Order object returned by buy/check/finish/cancel.</summary>
    internal sealed record OrderDto(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("phone")] string? Phone,
        [property: JsonPropertyName("operator")] string? Operator,
        [property: JsonPropertyName("product")] string? Product,
        [property: JsonPropertyName("price")] decimal? Price,
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("expires")] string? Expires,
        [property: JsonPropertyName("sms")] IReadOnlyList<SmsDto>? Sms);

    internal sealed record SmsDto(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("code")] string? Code,
        [property: JsonPropertyName("text")] string? Text,
        [property: JsonPropertyName("created")] string? Created);

    /// <summary>GET /v1/user/profile.</summary>
    internal sealed record ProfileDto(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("email")] string? Email,
        [property: JsonPropertyName("balance")] decimal Balance,
        [property: JsonPropertyName("default_country")] JsonElement DefaultCountry);

    /// <summary>
    /// GET /v1/guest/prices shape:
    /// <c>{ country: { product: { operator: { cost, count, rate } } } }</c>.
    /// Deserialized loosely because only one country/product branch is requested.
    /// </summary>
    internal static IReadOnlyList<(string Operator, decimal Cost, int Count)> ParsePriceBranch(JsonElement root)
    {
        var result = new List<(string, decimal, int)>();
        foreach (var country in root.EnumerateObject())
            foreach (var product in country.Value.EnumerateObject())
                foreach (var offer in product.Value.EnumerateObject())
                {
                    var cost = offer.Value.TryGetProperty("cost", out var c) && c.ValueKind == JsonValueKind.Number ? c.GetDecimal() : 0m;
                    var count = offer.Value.TryGetProperty("count", out var n) && n.ValueKind == JsonValueKind.Number ? n.GetInt32() : 0;
                    result.Add((offer.Name, cost, count));
                }

        return result;
    }

    internal static T? Deserialize<T>(string json) where T : class =>
        JsonSerializer.Deserialize<T>(json, JsonOptions);
}
