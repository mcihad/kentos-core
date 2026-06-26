using Kentos.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace Kentos.Infrastructure.DependencyInjection;

/// <summary>OpenTelemetry (traces + metrics + Prometheus) and Serilog wiring.</summary>
public static class ObservabilityExtensions
{
    public static IServiceCollection AddKentosObservability(
        this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(ObservabilityOptions.SectionName).Get<ObservabilityOptions>()
                      ?? new ObservabilityOptions();
        var hasOtlp = !string.IsNullOrWhiteSpace(options.OtlpEndpoint);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(options.ServiceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddSource("Npgsql")
                    .AddSource("Wolverine");

                if (hasOtlp)
                {
                    tracing.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(options.OtlpEndpoint!));
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddPrometheusExporter();

                if (hasOtlp)
                {
                    metrics.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(options.OtlpEndpoint!));
                }
            });

        return services;
    }

    /// <summary>Registers Serilog as the logging provider, reading from configuration.</summary>
    public static void UseKentosSerilog(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSerilog((serviceProvider, loggerConfiguration) => loggerConfiguration
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(serviceProvider)
            .Enrich.FromLogContext());
    }
}
