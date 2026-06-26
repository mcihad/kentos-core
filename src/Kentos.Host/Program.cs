using JasperFx.Resources;
using Kentos.Infrastructure.DependencyInjection;
using Kentos.Infrastructure.Modules;
using Kentos.Infrastructure.Persistence;
using Kentos.Modules.Hesap.Startup;
using Serilog;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

builder.UseKentosSerilog();

builder.Services.AddKentosInfrastructure(builder.Configuration);
builder.Services.AddKentosAudit(builder.Configuration);
builder.Services.AddKentosObservability(builder.Configuration);
builder.Services.AddKentosScheduling();
builder.Services.AddKentosAuthentication(builder.Configuration);
builder.Services.AddKentosAuthorization();
builder.Services.AddKentosApi(builder.Configuration);

// Discover and register the license-enabled modules.
var modules = ModuleLoader.DiscoverEnabledModules(builder.Configuration);
var mvcBuilder = builder.Services.AddControllers();
foreach (var module in modules)
{
    module.Register(builder.Services, builder.Configuration, builder.Environment);
    mvcBuilder.AddApplicationPart(module.GetType().Assembly);
}

var moduleAssemblies = modules.Select(m => m.GetType().Assembly).Distinct().ToArray();
builder.Services.AddSingleton(new ModuleRegistry(modules));
builder.Services.AddKentosMapping(moduleAssemblies);

var postgresConnectionString = builder.Configuration.GetConnectionString(PersistenceExtensions.PostgresConnectionName);
builder.Host.UseWolverine(options => MessagingExtensions.Configure(options, postgresConnectionString, moduleAssemblies));

// Create Wolverine's durable message-store schema/tables (and any other stateful
// resources) on startup so the outbox works in every environment.
builder.Services.AddResourceSetupOnStartup();

var app = builder.Build();

AuditExtensions.ConfigureAuditNet(app.Services);

app.UseKentosRequestLogging();

// Apply EF migrations on startup (default on). Set Database:MigrateOnStartup=false when
// migrations are applied out-of-band; the schema must already exist before startup then.
if (app.Configuration.GetValue("Database:MigrateOnStartup", true))
{
    await MigrationRunner.MigrateAllAsync(app.Services);
}

// Idempotent: upserts the permission catalog and bootstraps the admin role/user.
// Runs after migrations so the Hesap schema is guaranteed to exist.
await HesapSeeder.SeedAsync(app.Services);

app.UseKentosApi();

await app.RunAsync();

/// <summary>Exposed for the integration test WebApplicationFactory.</summary>
public partial class Program;
