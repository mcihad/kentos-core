using Asp.Versioning;
using Kentos.Infrastructure.OpenApi;
using Kentos.Infrastructure.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scalar.AspNetCore;

namespace Kentos.Infrastructure.DependencyInjection;

/// <summary>API surface wiring: controllers, versioning, CORS, OpenAPI/Scalar, health.</summary>
public static class ApiExtensions
{
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

        services.AddOpenApi("v1", options =>
        {
            options.AddOperationTransformer<PermissionOperationTransformer>();
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            options.AddDocumentTransformer<TagGroupsDocumentTransformer>();
        });

        var connectionString = configuration.GetConnectionString(PersistenceExtensions.PostgresConnectionName);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddHealthChecks().AddNpgSql(connectionString);
        }

        return services;
    }

    /// <summary>Wires the standard middleware pipeline and endpoints.</summary>
    public static WebApplication UseKentosApi(this WebApplication app)
    {
        app.UseForwardedHeaders(); // must run first so the real client IP/scheme are seen downstream
        app.UseExceptionHandler();
        app.UseCors(CorsOptions.PolicyName);
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapPrometheusScrapingEndpoint();
        app.MapHealthChecks("/health/live");
        app.MapHealthChecks("/health/ready");

        app.MapOpenApi();

        app.MapScalarApiReference(options =>
        {
            // Sidebar starts collapsed: expand module group → resource → method.
            options.DefaultOpenAllTags = false;
            options
                .AddPreferredSecuritySchemes("Bearer")
                .AddHttpAuthentication("Bearer", _ => { });
        });

        return app;
    }
}
