using System;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

public static class TraceIdentifierLoggerConfigurationExtensions
{
    /// <summary>
    /// Enrich log events with a TraceIdentifier property containing the current TraceIdentifier/>.
    /// </summary>
    /// <param name="enrichmentConfiguration">Logger enrichment configuration.</param>
    /// <returns>Configuration object allowing method chaining.</returns>
    public static LoggerConfiguration WithTraceIdentifier(
       this LoggerEnrichmentConfiguration enrichmentConfiguration)
    {
        if (enrichmentConfiguration == null) throw new ArgumentNullException(nameof(enrichmentConfiguration));
        return enrichmentConfiguration.With<TraceIdentifierEnricher>();
    }
}

class TraceIdentifierEnricher
        : ILogEventEnricher
{
    private const string TraceIdentifierPropertyName = "TraceIdentifier";

    public TraceIdentifierEnricher()
    {
    }


    /// <summary>
    /// Enrich the log event.
    /// </summary>
    /// <param name="logEvent">The log event to enrich.</param>
    /// <param name="propertyFactory">Factory for creating new properties to add to the event.</param>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var property = propertyFactory.CreateProperty(TraceIdentifierPropertyName, System.Diagnostics.Activity.Current?.Id ?? "-");
        logEvent.AddOrUpdateProperty(property);
    }
}
