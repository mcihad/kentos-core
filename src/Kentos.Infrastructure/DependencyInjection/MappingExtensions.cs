using System.Reflection;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace Kentos.Infrastructure.DependencyInjection;

/// <summary>Mapster wiring: scans per-entity IRegister mappings and registers IMapper.</summary>
public static class MappingExtensions
{
    public static IServiceCollection AddKentosMapping(this IServiceCollection services, params Assembly[] assemblies)
    {
        var config = TypeAdapterConfig.GlobalSettings;
        if (assemblies.Length > 0)
        {
            config.Scan(assemblies);
        }

        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();
        return services;
    }
}
