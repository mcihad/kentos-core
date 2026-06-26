using Audit.Core;
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

        // KVKK: never persist secrets/PII-sensitive columns to the audit trail. Masked
        // centrally (regardless of entity) just before the audit event is saved.
        Audit.Core.Configuration.AddOnSavingAction(MaskSensitiveData);
    }

    /// <summary>Column-name fragments whose values must be masked in the audit trail.</summary>
    private static readonly string[] SensitiveColumnTokens =
    [
        "password", "parola", "hash", "secret",
        "securitystamp", "guvenlik_damgasi", "concurrencystamp", "eszamanlilik", "token",
    ];

    private static void MaskSensitiveData(AuditScope scope)
    {
        var efEvent = scope.GetEntityFrameworkEvent();
        if (efEvent is null)
        {
            return;
        }

        foreach (var entry in efEvent.Entries)
        {
            if (entry.ColumnValues is not null)
            {
                foreach (var column in entry.ColumnValues.Keys.ToList())
                {
                    if (IsSensitive(column))
                    {
                        entry.ColumnValues[column] = "***";
                    }
                }
            }

            if (entry.Changes is null)
            {
                continue;
            }

            foreach (var change in entry.Changes.Where(c => IsSensitive(c.ColumnName)))
            {
                change.OriginalValue = "***";
                change.NewValue = "***";
            }
        }
    }

    private static bool IsSensitive(string columnName) =>
        SensitiveColumnTokens.Any(token => columnName.Contains(token, StringComparison.OrdinalIgnoreCase));

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
