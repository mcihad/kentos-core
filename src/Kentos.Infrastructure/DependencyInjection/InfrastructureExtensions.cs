using Kentos.Infrastructure.Errors;
using Kentos.Infrastructure.Identity;
using Kentos.Infrastructure.Options;
using Kentos.Infrastructure.Persistence.Interceptors;
using Kentos.SharedKernel.Identity;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kentos.Infrastructure.DependencyInjection;

/// <summary>Core infrastructure services (options, identity, interceptors, error handling).</summary>
public static class InfrastructureExtensions
{
    public static IServiceCollection AddKentosInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<GeoOptions>(configuration.GetSection(GeoOptions.SectionName));
        services.Configure<AuditOptions>(configuration.GetSection(AuditOptions.SectionName));
        services.Configure<LicenseOptions>(configuration.GetSection(LicenseOptions.SectionName));
        services.Configure<CorsOptions>(configuration.GetSection(CorsOptions.SectionName));
        services.Configure<ObservabilityOptions>(configuration.GetSection(ObservabilityOptions.SectionName));

        services.AddHttpContextAccessor();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<IErrorRecorder, ErrorRecorder>();

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }
}
