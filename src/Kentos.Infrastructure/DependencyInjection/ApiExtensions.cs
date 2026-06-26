using System.Text.Json;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Kentos.Infrastructure.Modules;
using Kentos.Infrastructure.OpenApi;
using Kentos.Infrastructure.Options;
using Kentos.SharedKernel.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scalar.AspNetCore;

namespace Kentos.Infrastructure.DependencyInjection;

/// <summary>API surface wiring: controllers, versioning, CORS, OpenAPI/Scalar, health.</summary>
public static class ApiExtensions
{
    /// <summary>Rate-limit policy guarding the anonymous auth endpoints (brute-force).</summary>
    public const string AuthRateLimitPolicy = "auth";

    public static IServiceCollection AddKentosApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();

        // Honor X-Forwarded-* from the reverse proxy so the real client IP/scheme reach
        // the app (access logs, audit actor IP, redirects). KnownProxies/Networks are
        // cleared to trust the local proxy in dev; restrict these in production.
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddMvc()
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        var cors = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();
        services.AddCors(options => options.AddPolicy(CorsOptions.PolicyName, policy =>
        {
            if (cors.Origins.Count > 0)
            {
                policy.WithOrigins(cors.Origins.ToArray()).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
            }
            else
            {
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            }
        }));

        // Combined document: every module's endpoints, tagged by controller (default).
        services.AddOpenApi("v1", options =>
        {
            options.AddOperationTransformer<PermissionOperationTransformer>();
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        });

        // DB check is a *readiness* signal only (tag "ready"); liveness must not depend on it.
        var connectionString = configuration.GetConnectionString(PersistenceExtensions.PostgresConnectionName);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddHealthChecks().AddNpgSql(connectionString, tags: ["ready"]);
        }

        // Brute-force guard for the anonymous auth endpoints: per-client-IP fixed window.
        // The real client IP is correct because UseForwardedHeaders runs first in the pipeline.
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(AuthRateLimitPolicy, http =>
                RateLimitPartition.GetFixedWindowLimiter(
                    http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));

            // Keep rejections consistent with the app's RFC7807 error shape.
            options.OnRejected = async (context, ct) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                }

                await context.HttpContext.Response.WriteAsJsonAsync(
                    new { status = 429, title = "Çok fazla istek", errorCode = "rate_limited" },
                    (JsonSerializerOptions?)null, "application/problem+json", ct);
            };
        });

        return services;
    }

    /// <summary>
    /// Registers one OpenAPI document per enabled module (named by slug), each scoped to
    /// that module's <c>api/v*/{slug}/*</c> routes. Called after module discovery because
    /// the module set is only known at runtime. The combined "v1" document is registered
    /// separately in <see cref="AddKentosApi"/>.
    /// </summary>
    public static IServiceCollection AddKentosModuleDocs(this IServiceCollection services, IReadOnlyList<IModule> modules)
    {
        foreach (var module in modules)
        {
            var slug = module.Slug;
            services.AddOpenApi(slug, options =>
            {
                options.ShouldInclude = api => ModuleRoute.BelongsTo(api.RelativePath, slug);
                options.AddOperationTransformer<PermissionOperationTransformer>();
                options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            });
        }

        return services;
    }

    /// <summary>Wires the standard middleware pipeline and endpoints.</summary>
    public static WebApplication UseKentosApi(this WebApplication app)
    {
        app.UseForwardedHeaders(); // must run first so the real client IP/scheme are seen downstream
        app.UseExceptionHandler();
        app.UseCors(CorsOptions.PolicyName);
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapPrometheusScrapingEndpoint();

        // Liveness: process is up — no external dependencies (so a DB blip can't trigger a
        // pod restart). Readiness: dependencies (DB) are reachable — gate traffic on this.
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

        // Publishes /openapi/{documentName}.json for every registered document:
        // the combined "v1" plus one per module slug.
        app.MapOpenApi();

        // Combined Scalar across all modules (default controller-based tag list).
        app.MapScalarApiReference(options =>
        {
            options
                .AddPreferredSecuritySchemes("Bearer")
                .AddHttpAuthentication("Bearer", _ => { });
        });

        // A dedicated Scalar UI per module at /scalar/{slug}, backed by its own document.
        var registry = app.Services.GetRequiredService<ModuleRegistry>();
        foreach (var module in registry.EnabledModules)
        {
            var slug = module.Slug;
            app.MapScalarApiReference($"/scalar/{slug}", options =>
            {
                options
                    .WithOpenApiRoutePattern($"/openapi/{slug}.json")
                    .WithTitle($"{module.DisplayName} API")
                    .AddPreferredSecuritySchemes("Bearer")
                    .AddHttpAuthentication("Bearer", _ => { });
            });
        }

        // Docs home: a card grid linking to each module's Scalar UI. Excluded from the
        // OpenAPI description so it doesn't surface as a stray endpoint/tag in the doc.
        app.MapGet("/docs", (ModuleRegistry modules) =>
                Results.Content(DocsHomePage.Render(modules.EnabledModules), "text/html; charset=utf-8"))
            .AllowAnonymous()
            .ExcludeFromDescription();

        return app;
    }
}
