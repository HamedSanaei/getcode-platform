namespace GetCode.Application.Notifications;

/// <summary>
/// M04-008: outbound USER-SMS port (semantic operations). This is a DIFFERENT
/// concept from <c>IVirtualNumberProvider</c>: virtual numbers are purchased
/// inventory; this port sends SMS to the user's own phone. Adapters live in
/// Infrastructure and are replaceable without touching business logic.
/// </summary>
public interface ISmsNotificationPort
{
    /// <summary>Sends a templated verification code (e.g. login OTP) through the
    /// provider's approved verification-template mechanism when available.</summary>
    Task<SmsDeliveryResult> SendVerificationCodeAsync(
        VerificationCodeSmsRequest request,
        CancellationToken cancellationToken);

    /// <summary>Sends an ordinary transactional message where templates are not
    /// required (order status, wallet/security notifications).</summary>
    Task<SmsDeliveryResult> SendTransactionalSmsAsync(
        TransactionalSmsRequest request,
        CancellationToken cancellationToken);
}

public sealed record VerificationCodeSmsRequest(string RecipientE164, string Code);

public sealed record TransactionalSmsRequest(string RecipientE164, string MessageText);

/// <summary>Normalized delivery outcomes — vendor status strings never escape.</summary>
public enum SmsDeliveryOutcome
{
    Accepted = 0,
    Rejected = 1,
    InvalidRecipient = 2,
    InvalidTemplate = 3,
    AuthenticationFailed = 4,
    RateLimited = 5,
    ProviderUnavailable = 6,
    Timeout = 7,
    Unknown = 99,
}

public sealed record SmsDeliveryResult(
    SmsDeliveryOutcome Outcome,
    string SafeToken,
    bool IsTransientlyRetryable,
    long? ProviderMessageId = null)
{
    public static SmsDeliveryResult Accepted(long? providerMessageId = null) =>
        new(SmsDeliveryOutcome.Accepted, "accepted", IsTransientlyRetryable: false, providerMessageId);

    public static SmsDeliveryResult Failure(SmsDeliveryOutcome outcome, string token, bool retryable, long? providerMessageId = null) =>
        new(outcome, token, retryable, providerMessageId);
}

/// <summary>
/// M04-008: canonical Iranian mobile normalization. Lives in Application (not
/// inside any adapter) so every future notification provider shares one rule.
/// Canonical representation: E.164 international form WITH leading '+'
/// ("+989123456789").
/// </summary>
public static partial class IranianMobileNumber
{
    [System.Text.RegularExpressions.GeneratedRegex(@"^\+?(?:98|0098|0)?9\d{9}$", System.Text.RegularExpressions.RegexOptions.CultureInvariant)]
    private static partial System.Text.RegularExpressions.Regex MobilePattern();

    /// <summary>Normalizes to "+98XXXXXXXXXX"; returns null when not a valid Iranian mobile.</summary>
    public static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var candidate = raw.Trim()
            .Replace("(", string.Empty, StringComparison.Ordinal)
            .Replace(")", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);

        candidate = ConvertToAsciiDigits(candidate);
        return MobilePattern().IsMatch(candidate) ? $"+98{candidate[^10..]}" : null;
    }

    private static string ConvertToAsciiDigits(string input)
    {
        var sb = new System.Text.StringBuilder(input.Length);
        foreach (var ch in input)
        {
            sb.Append(ch switch
            {
                >= '۰' and <= '۹' => (char)('0' + (ch - '۰')),
                >= '٠' and <= '٩' => (char)('0' + (ch - '٠')),
                _ => ch,
            });
        }

        return sb.ToString();
    }
}
