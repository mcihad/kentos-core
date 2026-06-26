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

builder.Host.UseWolverine(options => MessagingExtensions.Configure(options, moduleAssemblies));

var app = builder.Build();

AuditExtensions.ConfigureAuditNet(app.Services);

app.UseKentosRequestLogging();

if (app.Environment.IsDevelopment())
{
    await MigrationRunner.MigrateAllAsync(app.Services);
}

// Idempotent: upserts the permission catalog and bootstraps the admin role/user.
await HesapSeeder.SeedAsync(app.Services);

app.UseKentosApi();

await app.RunAsync();

/// <summary>Exposed for the integration test WebApplicationFactory.</summary>
public partial class Program;
