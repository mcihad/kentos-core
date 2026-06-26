using System.Diagnostics;
using Kentos.Infrastructure.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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

    /// <summary>
    /// Serilog HTTP request logging enriched with the fields needed for access-log /
    /// audit purposes: client IP, user, user-agent, host and trace id. One structured
    /// line per request, written to every configured sink (Console + rolling File).
    /// </summary>
    public static WebApplication UseKentosRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} → {StatusCode} ({Elapsed:0.0} ms) from {ClientIp}";
            options.EnrichDiagnosticContext = (diagnostic, httpContext) =>
            {
                diagnostic.Set("ClientIp", httpContext.Connection.RemoteIpAddress?.ToString());
                diagnostic.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
                diagnostic.Set("RequestHost", httpContext.Request.Host.Value);
                diagnostic.Set("TraceId", Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier);

                var user = httpContext.User.FindFirst("preferred_username")?.Value;
                if (!string.IsNullOrEmpty(user))
                {
                    diagnostic.Set("UserName", user);
                }
            };
        });

        return app;
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
