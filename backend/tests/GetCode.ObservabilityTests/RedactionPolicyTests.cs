using GetCode.Infrastructure.Observability.Logging;

namespace GetCode.ObservabilityTests;

public sealed class RedactionPolicyTests
{
    [Theory]
    [InlineData("authorization")]
    [InlineData("password")]
    [InlineData("api-key")]
    [InlineData("refresh-token")]
    [InlineData("otp")]
    [InlineData("sms-body")]
    public void Sensitive_field_names_are_forbidden(string name)
    {
        Assert.Contains(name, LoggingRedactionPolicy.ForbiddenFieldNames);
    }

    [Fact]
    public void Phone_mask_does_not_return_full_number()
    {
        const string phone = "+14155552671";
        var masked = LoggingRedactionPolicy.MaskPhone(phone);
        Assert.NotEqual(phone, masked);
        Assert.Contains("****", masked);
    }
}
