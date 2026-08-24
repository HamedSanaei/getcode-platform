using System.Text.RegularExpressions;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace GetCode.Infrastructure.Observability.Logging;

public static partial class StructuredLogging
{
    [GeneratedRegex(@"[^A-Za-z0-9._-]+", RegexOptions.CultureInvariant)]
    private static partial Regex UnsafeInstanceChars();

    public static string GetInstanceId()
    {
        var raw = Environment.GetEnvironmentVariable("HOSTNAME") ?? Environment.MachineName;
        var safe = UnsafeInstanceChars().Replace(raw, "-").Trim('-');
        return string.IsNullOrWhiteSpace(safe) ? "unknown" : safe[..Math.Min(64, safe.Length)];
    }

    public static ILogger CreateBootstrapLogger(string serviceName, string instanceId, string rootPath, string environment, string appVersion)
    {
        var activeDirectory = Path.Combine(rootPath, "active", serviceName, instanceId);
        Directory.CreateDirectory(activeDirectory);
        var path = Path.Combine(activeDirectory, $"{serviceName}-.jsonl");
        var formatter = new JsonFormatter(renderMessage: true);

        return new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("service", serviceName)
            .Enrich.WithProperty("instance", instanceId)
            .Enrich.WithProperty("environment", environment)
            .Enrich.WithProperty("appVersion", appVersion)
            .Enrich.With<RedactionEnricher>()
            .WriteTo.Console(formatter)
            .WriteTo.File(
                formatter,
                path,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: null,
                fileSizeLimitBytes: 134_217_728,
                rollOnFileSizeLimit: true,
                shared: false,
                flushToDiskInterval: TimeSpan.FromSeconds(2))
            .CreateLogger();
    }
}
