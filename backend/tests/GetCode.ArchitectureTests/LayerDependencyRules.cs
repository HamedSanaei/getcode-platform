using NetArchTest.Rules;

namespace GetCode.ArchitectureTests;

/// <summary>
/// M00-002 enforcement of the Clean Architecture dependency direction and the
/// provider anti-corruption layer. Rules are IL-based (NetArchTest) so they catch
/// usage inside method bodies, not just assembly references.
/// The mapping between these rules and the architecture contract lives in
/// docs/architecture/BOUNDARIES.md ("Enforcement map").
/// </summary>
public sealed class LayerDependencyRules
{
    private static readonly string[] OuterLayerNamespaces =
    [
        "GetCode.Persistence",
        "GetCode.Infrastructure",
        "GetCode.Api",
        "GetCode.Worker",
    ];

    private static readonly string[] InfrastructureFrameworkNamespaces =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "Npgsql",
        "StackExchange.Redis",
        "Serilog",
    ];

    [Fact]
    public void Domain_depends_only_on_the_BCL()
    {
        var forbidden = new List<string>
        {
            "GetCode.Application",
            "GetCode.Contracts",
            "Microsoft.Extensions.Logging",
            "System.Net.Http",
        };
        forbidden.AddRange(OuterLayerNamespaces);
        forbidden.AddRange(InfrastructureFrameworkNamespaces);

        var result = Types.InAssembly(typeof(GetCode.Domain.AssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny([.. forbidden])
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Application_does_not_reach_outer_layers_or_infrastructure_frameworks()
    {
        var forbidden = new List<string>();
        forbidden.AddRange(OuterLayerNamespaces);
        forbidden.AddRange(InfrastructureFrameworkNamespaces);

        var result = Types.InAssembly(typeof(GetCode.Application.AssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny([.. forbidden])
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Contracts_are_self_contained_transport_types()
    {
        var result = Types.InAssembly(typeof(GetCode.Contracts.AssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "GetCode.Domain",
                "GetCode.Application",
                "GetCode.Persistence",
                "GetCode.Infrastructure",
                "GetCode.Api",
                "GetCode.Worker")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Persistence_implements_application_and_domain_only()
    {
        var result = Types.InAssembly(typeof(GetCode.Persistence.AssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "GetCode.Infrastructure",
                "GetCode.Api",
                "GetCode.Worker",
                "Microsoft.AspNetCore",
                "StackExchange.Redis")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Infrastructure_implements_application_and_domain_only()
    {
        var result = Types.InAssembly(typeof(GetCode.Infrastructure.AssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "GetCode.Persistence",
                "GetCode.Api",
                "GetCode.Worker",
                "Microsoft.AspNetCore",
                "Npgsql",
                "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    /// <summary>
    /// Provider ACL: provider-specific types (DTOs, status mappings, SDK shims) live under
    /// GetCode.Infrastructure.Providers and must never be referenced by the core layers.
    /// </summary>
    [Fact]
    public void Core_layers_never_reference_provider_adapter_namespaces()
    {
        var coreAssemblies = new[]
        {
            typeof(GetCode.Domain.AssemblyMarker).Assembly,
            typeof(GetCode.Application.AssemblyMarker).Assembly,
            typeof(GetCode.Contracts.AssemblyMarker).Assembly,
            typeof(GetCode.Persistence.AssemblyMarker).Assembly,
        };

        var result = Types.InAssemblies(coreAssemblies)
            .ShouldNot()
            .HaveDependencyOn("GetCode.Infrastructure.Providers")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    private static string Describe(NetArchTest.Rules.TestResult result) =>
        "Architecture rule violated by: " + string.Join(", ", result.FailingTypeNames ?? []);
}
