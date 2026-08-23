namespace GetCode.Infrastructure.Observability.Logging;

public static class LoggingRedactionPolicy
{
    public static readonly IReadOnlySet<string> ForbiddenFieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "authorization", "cookie", "set-cookie", "password", "passwd", "secret", "api-key", "apikey",
        "access-token", "accesstoken", "refresh-token", "refreshtoken", "otp", "sms-body", "smsbody"
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
