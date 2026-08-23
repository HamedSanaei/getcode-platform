using GetCode.Application.Providers;

namespace GetCode.UnitTests;

public sealed class ProviderContractShapeTests
{
    [Fact]
    public void Provider_errors_do_not_expose_raw_provider_payloads()
    {
        var properties = typeof(ProviderResult).GetProperties().Select(x => x.Name).ToArray();
        Assert.DoesNotContain("RawResponse", properties);
        Assert.DoesNotContain("ResponseBody", properties);
    }
}
