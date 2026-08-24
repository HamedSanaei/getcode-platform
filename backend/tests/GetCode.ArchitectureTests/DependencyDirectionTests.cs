using System.Reflection;

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

public sealed class ProviderNotificationSeparationTests
{
    [Fact]
    public void Virtual_number_and_sms_notification_adapters_never_reference_each_other()
    {
        var infrastructure = typeof(GetCode.Infrastructure.DependencyInjection).Assembly;
        var fiveSimTypes = infrastructure.GetTypes()
            .Where(t => t.FullName?.StartsWith("GetCode.Infrastructure.Providers.FiveSim", StringComparison.Ordinal) == true)
            .Select(t => t.FullName!)
            .ToHashSet(StringComparer.Ordinal);
        var kavenegarTypes = infrastructure.GetTypes()
            .Where(t => t.FullName?.StartsWith("GetCode.Infrastructure.Notifications.Sms.Kavenegar", StringComparison.Ordinal) == true)
            .Select(t => t.FullName!)
            .ToHashSet(StringComparer.Ordinal);

        Assert.NotEmpty(fiveSimTypes);
        Assert.NotEmpty(kavenegarTypes);

        static bool ReferencesAny(Type type, HashSet<string> forbidden)
        {
            var touched = new List<string>();
            if (type.BaseType?.FullName is { } baseName) touched.Add(baseName);
            touched.AddRange(type.GetInterfaces().Select(i => i.FullName ?? string.Empty));
            touched.AddRange(type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).Select(f => f.FieldType.FullName ?? string.Empty));
            return touched.Any(name => forbidden.Contains(name));
        }

        foreach (var type in infrastructure.GetTypes())
        {
            if (type.FullName?.StartsWith("GetCode.Infrastructure.Notifications.Sms.Kavenegar", StringComparison.Ordinal) == true)
            {
                Assert.False(ReferencesAny(type, fiveSimTypes), $"{type.FullName} must not reference the virtual-number adapter");
            }

            if (type.FullName?.StartsWith("GetCode.Infrastructure.Providers.FiveSim", StringComparison.Ordinal) == true)
            {
                Assert.False(ReferencesAny(type, kavenegarTypes), $"{type.FullName} must not reference the SMS notification adapter");
            }
        }
    }
}
