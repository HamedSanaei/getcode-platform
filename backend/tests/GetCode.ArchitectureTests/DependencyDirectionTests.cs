namespace GetCode.ArchitectureTests;

public sealed class DependencyDirectionTests
{
    private static readonly string[] ForbiddenOuterPrefixes =
    [
        "GetCode.Persistence",
        "GetCode.Infrastructure",
        "GetCode.Api",
        "GetCode.Worker",
        "Microsoft.EntityFrameworkCore",
        "Npgsql",
        "StackExchange.Redis",
        "Serilog",
    ];

    [Fact]
    public void Domain_does_not_reference_outer_layers_or_infrastructure_frameworks()
    {
        var references = typeof(GetCode.Domain.AssemblyMarker).Assembly.GetReferencedAssemblies().Select(x => x.Name ?? string.Empty).ToArray();
        Assert.DoesNotContain(references, reference => ForbiddenOuterPrefixes.Any(prefix => reference.StartsWith(prefix, StringComparison.Ordinal)));
    }

    [Fact]
    public void Application_does_not_reference_outer_GetCode_layers()
    {
        var references = typeof(GetCode.Application.AssemblyMarker).Assembly.GetReferencedAssemblies().Select(x => x.Name ?? string.Empty).ToArray();
        var forbidden = new[] { "GetCode.Persistence", "GetCode.Infrastructure", "GetCode.Api", "GetCode.Worker" };
        Assert.DoesNotContain(references, reference => forbidden.Contains(reference, StringComparer.Ordinal));
    }
}
