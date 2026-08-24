namespace GetCode.Infrastructure.Observability.Logging;

public static class LoggingRedactionPolicy
{
    public static readonly IReadOnlySet<string> ForbiddenFieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        // Credentials & auth headers
        "authorization", "proxy-authorization", "cookie", "set-cookie", "password", "passwd",
        "secret", "client-secret", "api-key", "apikey", "x-api-key",
        "access-token", "accesstoken", "refresh-token", "refreshtoken", "id-token", "idtoken",
        "bearer", "jwt",
        // Provider/payment secrets
        "provider-token", "providertoken", "payment-credentials", "card-number", "cardnumber",
        "pan", "cvv", "cvc",
        // Customer-sensitive content
        "otp", "sms-body", "smsbody",
    };

    public static string MaskPhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 7)
        {
            return "***";
        }

        return $"{value[..Math.Min(4, value.Length)]}****{value[^3..]}";
    }
}
