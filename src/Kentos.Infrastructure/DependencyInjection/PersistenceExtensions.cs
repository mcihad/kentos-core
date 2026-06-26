using Audit.EntityFramework;
using Kentos.Infrastructure.Persistence;
using Kentos.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kentos.Infrastructure.DependencyInjection;

/// <summary>EF Core registration helpers for module DbContexts and the auditing context.</summary>
public static class PersistenceExtensions
{
    public const string PostgresConnectionName = "Postgres";

    /// <summary>
    /// Registers a module DbContext with Npgsql + NetTopologySuite + snake_case naming,
    /// plus the auditable-entity and Audit.NET interceptors.
    /// </summary>
    public static IServiceCollection AddModuleDbContext<TContext>(
        this IServiceCollection services, IConfiguration configuration)
        where TContext : DbContext
    {
        var connectionString = configuration.GetConnectionString(PostgresConnectionName);

        services.AddDbContext<TContext>((sp, options) =>
        {
            options
                .UseNpgsql(connectionString, npgsql => npgsql.UseNetTopologySuite())
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(
                    sp.GetRequiredService<AuditableEntityInterceptor>(),
                    sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        services.AddSingleton(new DbContextRegistration(typeof(TContext)));
        return services;
    }

    /// <summary>Registers the auditing/error-log DbContext (no audit interceptor → no recursion).</summary>
    public static IServiceCollection AddAuditingDbContext(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(PostgresConnectionName);

        services.AddDbContext<AuditingDbContext>(options =>
        {
            options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention();
        });

        services.AddSingleton(new DbContextRegistration(typeof(AuditingDbContext)));
        return services;
    }
}

/// <summary>Marker recording a DbContext type so the host can migrate it at startup.</summary>
public sealed record DbContextRegistration(Type ContextType);
