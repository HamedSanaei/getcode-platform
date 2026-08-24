namespace GetCode.Persistence.Identity;

/// <summary>
/// Defense-in-depth: audit details are written by application code that already
/// avoids secrets, but the persistence adapter refuses obvious sensitive keys
/// outright so a future caller cannot leak them into the audit trail.
/// </summary>
public static class LoggingRedaction
{
    public static readonly IReadOnlySet<string> ForbiddenAuditKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "password", "passwd", "secret", "api-key", "apikey", "authorization", "cookie",
        "access-token", "refresh-token", "jwt", "bearer", "otp", "sms-body",
        "card-number", "cvv", "password-hash", "passwordhash",
    };
}
