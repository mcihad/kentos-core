---
name: create-module
description: Scaffold a brand-new Kentos module from scratch — class library project, IModule, module DbContext (own Turkish schema), design-time factory, permissions catalog, and wiring into the host, the admin CLI, and the license list. Use when asked to add/create/scaffold a new module.
---

# create-module

Creates an empty but fully-wired Kentos module following [agents.md](../../../agents.md).
After it builds, add resources with **/create-resource**. The canonical reference is
`src/Modules/Kentos.Modules.Settlement/` — mirror it.

> Naming: code identifiers English; DB names (schema/table/column) Turkish via mapping
> strings; DB comments (`HasComment`) Turkish; user-facing text (validation messages,
> endpoint summaries, permission titles, module `DisplayName`) Turkish.

## 0. Inputs (ask if missing)

- **Module name** — English PascalCase, e.g. `Catalog` → project
  `Kentos.Modules.Catalog`.
- **Route slug** — English lowercase, e.g. `catalog`.
- **DB schema** — Turkish, e.g. `katalog` (a DB object → Turkish).
- **License key** — usually the slug; `null` makes it a core (always-on) module.

Replace `{Module}` (Catalog), `{module}` (catalog), `{schema}` (katalog),
`{Ctx}` = `{Module}DbContext`.

## 1. Create the project & add to the solution

```bash
dotnet new classlib -o src/Modules/Kentos.Modules.{Module} -n Kentos.Modules.{Module} --no-restore
rm -f src/Modules/Kentos.Modules.{Module}/Class1.cs
dotnet sln Kentos.slnx add src/Modules/Kentos.Modules.{Module}
```

## 2. Replace the `.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\Kentos.Infrastructure\Kentos.Infrastructure.csproj" />
  </ItemGroup>
  <ItemGroup Label="EF migrations live in this module">
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

Create the canonical folders (see agents.md §3 for the full tree and §15 for the
hard compliance checklist): `Domain/Configurations`, `Application`, `Services`,
`Mappings`, `Events`, `Permissions`, `Infrastructure`, `Api`. Do not invent alternate
names (e.g. never `Dtos/` instead of `Application/`). `/create-resource` fills these.

## 3. Module DbContext — `Infrastructure/{Ctx}.cs`

```csharp
using Kentos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kentos.Modules.{Module}.Infrastructure;

public sealed class {Ctx} : KentosDbContext
{
    public const string Schema = "{schema}";

    public {Ctx}(DbContextOptions<{Ctx}> options) : base(options) { }

    // public DbSet<Foo> Foos => Set<Foo>();   // added by /create-resource

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        // modelBuilder.HasPostgresExtension("postgis"); // only if the module uses geometry
        modelBuilder.ApplyConfigurationsFromAssembly(typeof({Ctx}).Assembly);
        base.OnModelCreating(modelBuilder); // MUST be last (applies the soft-delete filter)
    }
}
```

## 4. Design-time factory — `Infrastructure/{Ctx}DesignFactory.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kentos.Modules.{Module}.Infrastructure;

public sealed class {Ctx}DesignFactory : IDesignTimeDbContextFactory<{Ctx}>
{
    public {Ctx} CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=5432;Database=kentos;Username=kentos;Password=kentos";
        var options = new DbContextOptionsBuilder<{Ctx}>()
            .UseNpgsql(cs, npgsql => npgsql.UseNetTopologySuite())
            .UseSnakeCaseNamingConvention()
            .Options;
        return new {Ctx}(options);
    }
}
```

## 5. Permissions catalog — `Permissions/{Module}Permissions.cs`

```csharp
using Kentos.SharedKernel.Authorization;

namespace Kentos.Modules.{Module}.Permissions;

public static class {Module}Permissions
{
    public const string ModuleSlug = "{module}";

    public static IReadOnlyList<PermissionDefinition> All { get; } = [];
    // /create-resource appends nested classes + Def("resource", PermissionAction.X, "Title") entries here.
}
```

## 6. The module — `{Module}Module.cs`

```csharp
using Kentos.Infrastructure.DependencyInjection;
using Kentos.Modules.{Module}.Infrastructure;
using Kentos.Modules.{Module}.Permissions;
using Kentos.SharedKernel.Authorization;
using Kentos.SharedKernel.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kentos.Modules.{Module};

public sealed class {Module}Module : IModule
{
    public string Slug => {Module}Permissions.ModuleSlug;
    public string DisplayName => "{Module}";
    public string Version => "1.0.0";
    public string? LicenseKey => "{module}";   // null = core module (always enabled)
    public IReadOnlyList<PermissionDefinition> Permissions => {Module}Permissions.All;

    public void Register(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddModuleDbContext<{Ctx}>(configuration);
        services.AddValidatorsFromAssembly(typeof({Module}Module).Assembly, includeInternalTypes: true);
        // /create-resource appends: services.AddScoped<IFooService, FooService>();
    }
}
```

The `DisplayName` is a Turkish label (e.g. "Yerleşim"), shown by the metadata API.

## 7. Wire it in

- `src/Kentos.Host/Kentos.Host.csproj`: add
  `<ProjectReference Include="..\Modules\Kentos.Modules.{Module}\Kentos.Modules.{Module}.csproj" />`.
- `tools/Kentos.AdminCli/Kentos.AdminCli.csproj`: add the same ProjectReference
  (so `permissions scan` discovers the module).
- Enable it: add the slug to `License:EnabledModules` in
  `src/Kentos.Host/appsettings.json` and `.env.example`
  (comma-separated, e.g. `"settlement,catalog"`).

## 8. Verify

```bash
make build                      # compiles
dotnet run --project tools/Kentos.AdminCli -- permissions scan -o permissions.json   # module discoverable (0 perms yet)
make run                        # GET /api/v1/metadata now lists the new module
```

Then add resources with **/create-resource** (module = `{module}`).

## Gotchas

- `base.OnModelCreating(modelBuilder)` must be the **last** line of the context's
  `OnModelCreating` (the soft-delete filter loop must see every configured entity).
- The module won't load at runtime unless its slug is in `License:EnabledModules`.
- Keep the module dependency-free of other modules — talk via SharedKernel
  contracts or Wolverine messages only.
