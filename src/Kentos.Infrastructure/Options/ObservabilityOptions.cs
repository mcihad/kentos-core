namespace Kentos.Infrastructure.Options;

/// <summary>OpenTelemetry / observability settings (the "OpenTelemetry" section).</summary>
public sealed class ObservabilityOptions
{
    public const string SectionName = "OpenTelemetry";

    /// <summary>OTLP collector endpoint (e.g. http://localhost:4317). Empty disables OTLP export.</summary>
    public string? OtlpEndpoint { get; set; }

    public string ServiceName { get; set; } = "kentos";
}
