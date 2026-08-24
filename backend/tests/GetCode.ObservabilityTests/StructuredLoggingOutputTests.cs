using System.Text.Json;
using GetCode.Infrastructure.Observability.Logging;
using Serilog;
using Serilog.Context;
using Xunit;

namespace GetCode.ObservabilityTests;

/// <summary>
/// M00-007 verification: the real bootstrap logger writes compact JSONL with
/// service/environment context, flows correlationId from LogContext, and the
/// redaction enricher masks forbidden properties before any sink sees them.
/// </summary>
public sealed class StructuredLoggingOutputTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "gc-logtests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void Bootstrap_logger_writes_jsonl_with_context_and_redaction()
    {
        var logger = StructuredLogging.CreateBootstrapLogger(
            "getcode-test", "inst-1", _root, "Testing", "1.2.3.0");

        using (LogContext.PushProperty("correlationId", "corr-123"))
        {
            // Forbidden property names must never survive into any sink.
            logger.Information("probe.event {orderId} {password} {authorization}", "ord_1", "hunter2", "Bearer abc.def.ghi");
        }

        // Dispose the instance logger so the file sink releases its exclusive handle.
        (logger as IDisposable)?.Dispose();

        var activeDir = Path.Combine(_root, "active", "getcode-test", "inst-1");
        var file = Directory.EnumerateFiles(activeDir, "*.jsonl").Single();
        var line = File.ReadLines(file).Single();
        var json = JsonDocument.Parse(line).RootElement;
        var properties = json.GetProperty("Properties");

        // Enriched context lives under "Properties"; the event shape stays flat.
        Assert.Equal("getcode-test", properties.GetProperty("service").GetString());
        Assert.Equal("Testing", properties.GetProperty("environment").GetString());
        Assert.Equal("1.2.3.0", properties.GetProperty("appVersion").GetString());
        Assert.Equal("corr-123", properties.GetProperty("correlationId").GetString());
        Assert.Contains("probe.event", json.GetProperty("RenderedMessage").GetString());

        Assert.Equal("ord_1", properties.GetProperty("orderId").GetString());
        Assert.Equal(RedactionEnricher.MaskedValue, properties.GetProperty("password").GetString());
        Assert.Equal(RedactionEnricher.MaskedValue, properties.GetProperty("authorization").GetString());
        Assert.DoesNotContain("hunter2", line);
        Assert.DoesNotContain("Bearer abc.def.ghi", line);
    }

    [Fact]
    public void Instance_ids_are_filesystem_safe()
    {
        var id = StructuredLogging.GetInstanceId();
        Assert.NotEqual("unknown", id);
        Assert.DoesNotMatch(@"[^A-Za-z0-9._-]", id);
    }
}
