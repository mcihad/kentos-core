using Audit.EntityFramework;
using Kentos.Infrastructure.Auditing;
using Kentos.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Kentos.Infrastructure.DependencyInjection;

/// <summary>Audit.NET wiring (EF interceptor + Postgres/Mongo writer).</summary>
public static class AuditExtensions
{
    public static IServiceCollection AddKentosAudit(this IServiceCollection services, IConfiguration configuration)
    {
        var auditOptions = configuration.GetSection(AuditOptions.SectionName).Get<AuditOptions>() ?? new AuditOptions();

        services.AddAuditingDbContext(configuration);
        services.AddSingleton<AuditSaveChangesInterceptor>();

        if (auditOptions.Provider == AuditProvider.Mongo)
        {
            var mongoConnection = configuration.GetConnectionString("Mongo");
            var mongoDatabase = configuration["Mongo:Database"] ?? "kentos_audit";

            services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnection));
            services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoDatabase));
            services.AddScoped<IAuditWriter, MongoAuditWriter>();
            RegisterMongoClassMaps();
        }
        else
        {
            services.AddScoped<IAuditWriter, PostgresAuditWriter>();
        }

        return services;
    }

    /// <summary>Applies global Audit.NET configuration. Call once after the host is built.</summary>
    public static void ConfigureAuditNet(IServiceProvider serviceProvider)
    {
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        Audit.Core.Configuration.Setup()
            .UseCustomProvider(new KentosAuditDataProvider(scopeFactory))
            .WithCreationPolicy(Audit.Core.EventCreationPolicy.InsertOnEnd);

        Audit.EntityFramework.Configuration.Setup()
            .ForAnyContext(config => config.IncludeEntityObjects());
    }

    private static void RegisterMongoClassMaps()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(AuditLog)))
        {
            BsonClassMap.RegisterClassMap<AuditLog>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
                cm.UnmapMember(c => c.Id);
            });
        }
    }
}
