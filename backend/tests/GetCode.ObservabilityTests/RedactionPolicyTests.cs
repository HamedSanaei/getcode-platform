using GetCode.Infrastructure.Observability.Logging;

namespace GetCode.ObservabilityTests;

public sealed class RedactionPolicyTests
{
    // AGENTS.md / ADR-012 forbid logging these categories. Each category must be
    // represented in the enforced forbidden-field set so the RedactionEnricher
    // masks them before any sink renders the value.
    [Theory]
    [InlineData("password")]            // passwords
    [InlineData("client-secret")]       // API keys / secrets
    [InlineData("api-key")]
    [InlineData("x-api-key")]
    [InlineData("authorization")]       // authorization headers
    [InlineData("proxy-authorization")]
    [InlineData("jwt")]                 // JWTs / bearer tokens
    [InlineData("bearer")]
    [InlineData("access-token")]
    [InlineData("refresh-token")]       // refresh tokens
    [InlineData("cookie")]              // cookies
    [InlineData("set-cookie")]
    [InlineData("provider-token")]      // provider tokens
    [InlineData("payment-credentials")] // payment credentials
    [InlineData("card-number")]
    [InlineData("cvv")]
    [InlineData("otp")]                 // raw OTPs
    [InlineData("sms-body")]            // raw SMS bodies
    public void Sensitive_categories_are_forbidden(string name)
    {
        Assert.Contains(name, LoggingRedactionPolicy.ForbiddenFieldNames);
    }

    [Fact]
    public void Forbidden_names_match_case_insensitively()
    {
        Assert.Contains("PASSWORD", LoggingRedactionPolicy.ForbiddenFieldNames);
        Assert.Contains("Refresh-Token", LoggingRedactionPolicy.ForbiddenFieldNames);
    }

    [Fact]
    public void Phone_mask_does_not_return_full_number()
    {
        const string phone = "+14155552671";
        var masked = LoggingRedactionPolicy.MaskPhone(phone);
        Assert.NotEqual(phone, masked);
        Assert.Contains("****", masked);
        Assert.DoesNotContain("5555", masked);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123456")]
    public void Phone_mask_fails_closed_on_short_or_missing_values(string? value)
    {
        Assert.Equal("***", LoggingRedactionPolicy.MaskPhone(value));
    }
}
