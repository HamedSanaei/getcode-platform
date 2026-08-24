using Serilog.Core;
using Serilog.Events;

namespace GetCode.Infrastructure.Observability.Logging;

/// <summary>
/// Enforces ADR-012 at the pipeline level: any structured-log property whose
/// name is in <see cref="LoggingRedactionPolicy.ForbiddenFieldNames"/> has its
/// value replaced before any sink renders it. Defense in depth against
/// accidental secret leakage; deliberate diagnostic modes must use explicitly
/// approved sanitized field names instead.
/// </summary>
public sealed class RedactionEnricher : ILogEventEnricher
{
    internal const string MaskedValue = "***redacted***";

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        foreach (var propertyName in logEvent.Properties.Keys)
        {
            if (!LoggingRedactionPolicy.ForbiddenFieldNames.Contains(propertyName))
            {
                continue;
            }

            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(propertyName, MaskedValue));
        }
    }
}
