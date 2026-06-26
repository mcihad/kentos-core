# Kentos Core — Architecture Contract (agents.md)

This file is the **authoritative architecture contract**. Any human or AI agent
working in this repository must follow it so the codebase stays consistent.

Kentos Core is a **Keycloak-secured, permission-driven, modular monolith** built
with **.NET 10**. Modules are loaded dynamically by license; every module owns its
own `DbContext`, Postgres schema, routes and permission set. All cross-cutting
concerns (auth, audit, soft-delete, telemetry, logging, exceptions, mapping,
validation, CQRS, scheduling, pagination, API docs) live in the shared
infrastructure layer.

---

## 1. The ONE naming rule (read this first)

**All code is English. Turkish appears ONLY as physical database object names
(schema / table / column), and ONLY inside EF mapping strings.**

| Concern | Language | Example |
|---|---|---|
| Namespaces, classes, entities, **fields**, methods, variables, filter names, comments | **English** | `Neighborhood { Name, DistrictId }` |
| DTO field names → JSON keys | **English** camelCase | `{ "name": "...", "districtId": "..." }` |
| Routes, slugs, permission keys, config keys | **English** | `settlement.neighborhood.create` |
| DB schema / table / column names | **Turkish** (via `HasDefaultSchema` / `ToTable` / `HasColumnName` only) | `yerlesim.mahalleler { ad, ilce_id }` |
| **DB comments** (`HasComment(...)`) | **Turkish** (they describe the Turkish schema) | `"Mahalle adı"` |
| **User-facing text** (FluentValidation messages, `[EndpointSummary]`, permission `title`, module `DisplayName`, user-facing exception labels) | **Turkish** | `"Ad zorunludur."`, `"Mahalle ekle"` |

- Tables are **plural**, snake_case, Turkish: `iller`, `ilceler`, `mahalleler`.
- Every table and every column **must** have `.HasComment(...)` (English).
- The English↔Turkish bridge for each entity lives **only** in its
  `IEntityTypeConfiguration`. Code identifiers and XML/code comments stay English;
  Turkish is allowed **only** in the buckets above (DB names, DB comments, user-facing text).
- Domain vocabulary is translated to English: `Il→Province`, `Ilce→District`,
  `Mahalle→Neighborhood`, schema `yerlesim`, slug `settlement`.

Permission keys are `module.resource.action`; standard actions are
`list, view, create, update, delete` (see `PermissionAction`).

---

## 2. Stack & versions

- **.NET 10** (`global.json` pins SDK; `net10.0`). Solution: `Kentos.slnx`.
- **PostgreSQL 18 + PostGIS** (`uuidv7()` used as a DB default fallback;
  `Guid.CreateVersion7()` is the primary, app-side generator).
- EF Core 10 + `Npgsql.EntityFrameworkCore.PostgreSQL` (+ NetTopologySuite),
  `EFCore.NamingConventions` (snake_case fallback).
- **Wolverine** (CQRS mediator + messaging) — handlers are static `Handle` methods.
- **Mapster** (per-entity `IRegister`), **FluentValidation** (per command/query).
- **Audit.NET** (EF Core interceptor) → `AuditLog`; provider Postgres (dev) / Mongo (prod).
- **OpenTelemetry** (traces + metrics + Prometheus `/metrics`) + **Serilog**.
- **Scalar** + built-in `Microsoft.AspNetCore.OpenApi`; **Asp.Versioning**; **Quartz**.
- Central Package Management (`Directory.Packages.props`); shared MSBuild
  (`Directory.Build.props`, nullable warnings are errors).

---

## 3. Solution layout

```
src/
  Kentos.SharedKernel/   BaseEntity, IEntity/IAuditable/ISoftDeletable, Result,
                         PagedRequest/PagedResponse, ICommand/IQuery,
                         RequiresPermissionAttribute, PermissionDefinition,
                         IModule, ModuleManifest, exceptions, ICurrentUser
  Kentos.Infrastructure/ EF base + interceptors, AuditLog/ErrorLog, Audit.NET,
                         GlobalExceptionHandler, Keycloak auth + permission policy,
                         OpenTelemetry/Serilog, Mapster/Quartz wiring, module loader,
                         OpenAPI transformers, DI extensions (AddKentos*)
  Kentos.Host/           Program.cs (dynamic module loading), MetadataController
  Modules/
    Kentos.Modules.Settlement/  Domain, Application, Infrastructure, Api, Mappings,
                                Permissions, SettlementModule
tools/Kentos.AdminCli/   Spectre.Console.Cli: provision + permissions scan/sync
tests/                   TestShared (Testcontainers + TestAuthHandler) + unit + integration
```

Modules **never** reference each other. They reference `SharedKernel` +
`Infrastructure` and communicate only via SharedKernel contracts or Wolverine
messages. Each module maps to its own Postgres schema.

---

## 4. Entities & persistence

### BaseEntity (`SharedKernel/Entities/BaseEntity.cs`)
Abstract → not a table → English name. English fields mapped to Turkish columns:

| Field | Column | Field | Column |
|---|---|---|---|
| `Id` (long, identity) | `id` | `CreatedBy` | `olusturan` |
| `Uuid` (Guid, public id) | `uuid` | `CreatedAt` | `olusturma_tarihi` |
| `Version` (concurrency) | `surum` | `UpdatedBy` | `guncelleyen` |
| `Metadata` (jsonb) | `meta_veri` | `UpdatedAt` | `guncelleme_tarihi` |
| `IsDeleted` | `silindi_mi` | `DeletedBy` | `silen` |
| `DeletedAt` | `silme_tarihi` | | |

- `Id` is internal (never exposed). The API uses `Uuid`, surfaced as `id` in DTOs.
- Two SaveChanges interceptors (registered via `AddModuleDbContext`):
  - `AuditableEntityInterceptor`: sets `Uuid` (`CreateVersion7`), `CreatedBy/At`,
    `UpdatedBy/At`, `Version++`, and converts hard delete → soft delete.
  - Audit.NET `AuditSaveChangesInterceptor`: records mutations to `AuditLog`.
- **Soft delete** uses an EF Core 10 **named query filter** `"SoftDelete"`
  (`KentosDbContext`); bypass with `IgnoreQueryFilters("SoftDelete")`.
- `EntityConfigurationExtensions.ConfigureBase<T>()` configures all base columns
  (Turkish names + comments). **Every entity config calls it.**

### Module DbContext
Derives from `KentosDbContext`, sets `HasDefaultSchema("<turkish>")`,
`HasPostgresExtension("postgis")` if geo, `ApplyConfigurationsFromAssembly(...)`,
then calls `base.OnModelCreating(...)` **last** (so the soft-delete filter sees all
entities). Register with `services.AddModuleDbContext<TContext>(configuration)`.
A design-time `IDesignTimeDbContextFactory` enables `dotnet ef`.

### Audit & error tables (`denetim` schema)
- `AuditLog` → `denetim.denetim_kayitlari`; `ErrorLog` → `denetim.hata_kayitlari`.
  English entities, Turkish columns. They do **not** derive from `BaseEntity` and
  are excluded from auditing (no recursion). Held by `AuditingDbContext`.

---

## 5. Reads, writes, services & Wolverine

The role split — and the reason Wolverine is in the stack:

- **Services** (`Services/I{Resource}Service` + impl, registered scoped in the module's
  `Register`) hold all data/business logic (DbContext, Mapster, geometry). Reusable,
  directly callable, unit-test friendly.
- **Reads (queries) bypass Wolverine.** The controller injects the service and calls it
  directly (`service.GetByIdAsync` / `ListAsync`). No message-bus ceremony for reads;
  list/query records are plain `[FromQuery]` request classes.
- **Writes (commands) go through Wolverine** (`bus.InvokeAsync<T>(command)`):
  1. Wolverine runs the FluentValidation middleware → `ValidationException` → 400.
  2. The command handler (static `Handle`) calls the service, then **publishes a domain
     event**: `await bus.PublishAsync(new XCreated(...))`.
  3. A write with neither validation nor an event (e.g. `delete`) calls the service
     directly instead — don't route it through the bus just for ceremony.
- **Domain events** are records (module-local, or in SharedKernel when cross-module).
  **Consumers** are **instance** classes named `{Event}Handler` with a single
  `Handle({Event} message, deps...)`. Wolverine routes the published event to the
  decoupled consumer via a local queue — the producer never references it. This is the
  modular-monolith decoupling mechanism (read-model updates, cache invalidation,
  notifications, cross-module reactions).
  - Discovery is by type-name suffix: a consumer class **must** end in `Handler`/`Consumer`
    and have a single `Handle` (avoid several overloaded `Handle` methods in one class —
    they are not discovered).
- Events are **in-process** today; for production reliability enable the Postgres
  transactional outbox (`PersistMessagesWithPostgresql` + EF integration) so the event is
  persisted in the same transaction as the write.
- `MessagingExtensions.Configure` sets `ServiceLocationPolicy.AlwaysAllowed`,
  `UseFluentValidation(RegistrationBehavior.ExplicitRegistration)` (validators are
  registered once per module via `AddValidatorsFromAssembly`; explicit registration
  avoids duplicate validation messages), and `IncludeAssembly(moduleAssembly)` **once**.
  Runtime codegen requires `WolverineFx.RuntimeCompilation`.

One file per use-case (`CreateNeighborhood.cs` = command + validator + handler). DTOs in
`<Resource>Response.cs`; events in `Events/`.

---

## 6. Authorization (Keycloak)

- Each permission is a **client role** on `kentos-client`. A protocol mapper emits
  them into the access token as a multivalued **`permissions`** claim.
- `[RequiresPermission("settlement.neighborhood.create")]` (a subclass of
  `AuthorizeAttribute`) sets policy `perm:<key>`.
- `PermissionPolicyProvider` materializes a policy per `perm:` prefix;
  `PermissionAuthorizationHandler` checks the `permissions` claim locally
  (no Keycloak round-trip). Missing permission → **403**; no token → **401**.
- `AddKentosAuthentication` configures JwtBearer (`MapInboundClaims=false`,
  audience `kentos-client`); `AddKentosAuthorization` registers the provider/handler.

---

## 7. API surface

- Controllers (not minimal API). Route:
  `/api/v{version:apiVersion}/{module-slug}/{resource}` e.g.
  `/api/v1/settlement/neighborhoods`.
- `Asp.Versioning` URL-segment versioning, default `v1`.
- Lists return `PagedResponse<T>` from a `PagedRequest` (page/pageSize/sort/search);
  pagination is mandatory.
- Errors are **RFC7807 ProblemDetails** with `errorCode`, `traceId`, and `errors`
  (validation). `GlobalExceptionHandler` maps exceptions (see `KentosException`
  subclasses: 404/409/422/403; `ValidationException`→400; otherwise 500) and
  persists 5xx as `ErrorLog` folded by fingerprint (`IErrorRecorder`).
- Each action has `[RequiresPermission(...)]` and `[EndpointSummary("...")]`.
- OpenAPI: `PermissionOperationTransformer` adds `x-required-permission` (visible in
  generated clients); `KeycloakSecuritySchemeTransformer` adds the OAuth2 scheme;
  Scalar UI at `/scalar`.
- `/api/v1/metadata` lists licensed modules + permissions;
  `/api/v1/metadata/{slug}` returns one manifest.
- `/metrics` (Prometheus), `/health/live`, `/health/ready`.

---

## 8. Modules & licensing

`IModule` (Slug, DisplayName, Version, LicenseKey, Permissions, Register). At
startup `ModuleLoader.DiscoverEnabledModules` loads `Kentos.Modules.*.dll`, keeps
modules whose `LicenseKey` is null (core) or whose slug is in
`License:EnabledModules`. The host calls `module.Register(...)`, adds the module
assembly as an MVC `ApplicationPart` (so its controllers activate only when
licensed), registers `ModuleRegistry`, scans Mapster `IRegister`s, and includes the
assembly in Wolverine discovery.

---

## 9. Observability, scheduling, audit provider

- OpenTelemetry traces (AspNetCore/HttpClient/EF/Npgsql/Wolverine) + metrics
  (+ Prometheus), OTLP export when `OpenTelemetry:OtlpEndpoint` is set.
- Serilog via `UseKentosSerilog` (config-driven) + request logging.
- Quartz via `AddKentosScheduling` (sample job included).
- Audit provider chosen by `Audit:Provider` (`Postgres`/`Mongo`); `AddKentosAudit`
  wires the writer; `AuditExtensions.ConfigureAuditNet(app.Services)` is called once
  after build.

---

## 10. AdminCli (`tools/Kentos.AdminCli`)

Reads Keycloak settings from `.env`/env (`KeycloakConfig`). Commands:
- `provision [--dev]` — idempotent: realm, client (public + PKCE), audience +
  permissions mappers, client roles (from `permissions.json`), `kentos-administrators`
  group, and (with `--dev`) a test user. Logs every successful stage to console and
  `keycloak-provision.log`.
- `permissions scan -o permissions.json` — reflects modules → writes the catalog.
- `permissions sync` — creates/updates client roles from `permissions.json`
  (separate fast-dev command).

`permissions.json` is generated from code (each module's `Permissions`). The
permission key is the single source of truth shared by the attribute, OpenAPI, and
Keycloak roles.

---

## 11. Tests

- `Kentos.TestShared`: `ApiFactory` (`WebApplicationFactory<Program>` + a PostGIS
  Testcontainer) and `TestAuthHandler` (reads `X-Test-User` / `X-Test-Permissions`).
- **Critical:** under `WebApplicationFactory` + minimal hosting,
  `ConfigureAppConfiguration` does **not** override `appsettings.json`. The test
  container connection string is injected via **environment variables** in
  `InitializeAsync` (`ConnectionStrings__Postgres`, `OpenTelemetry__OtlpEndpoint=""`).
  Follow this pattern for any new test config override.
- Integration tests use `[Collection("api")]` with a local `[CollectionDefinition]`
  in the **same assembly** (xUnit requirement).
- Unit tests cover validators, Mapster config, pagination. All 21 tests are green.

---

## 12. Recipes

These are automated by the skills `/create-module`, `/create-resource`,
`/update-resource`, `/create-permission` (in `.claude/skills/`) and verified with the
`make` targets. Use them; the steps below are the underlying contract.

### Add an entity to a module
1. `Domain/<Entity>.cs` : `BaseEntity` (English class + fields).
2. `Domain/Configurations/<Entity>Configuration.cs`: `ToTable("<turkish_plural>")`,
   `builder.ConfigureBase()`, then `HasColumnName("<turkish>")` + `HasComment(...)`
   for every property; relationships + indexes.
3. Add `DbSet` to the module `DbContext`.
4. `dotnet ef migrations add <Name> --project <module> --startup-project Kentos.Host --context <Ctx> -o Infrastructure/Migrations`.

### Add a use-case + endpoint
1. `Services/<Resource>Service.cs`: add the method (the actual logic) to
   `I<Resource>Service` + impl. `<Resource>Response.cs` (English fields) +
   `Mappings/<Resource>Mapping.cs` (`Uuid`→`Id`, parent FK→parent `Uuid`).
2. **Read** (get/list): controller injects the service and calls it directly — no
   command/handler. List uses a plain `[FromQuery]` request class.
3. **Write** (create/update): `Application/<Resource>/<Verb><Resource>.cs` = command
   record (`ICommand<T>`) + `AbstractValidator` (**Turkish** messages) + static `Handler`
   that calls the service and, for creates, `bus.PublishAsync(new <Resource>Created(...))`.
   A consumer `Events/<Resource>CreatedHandler.cs` (instance class) reacts. A write with
   no validation/event (delete) calls the service directly.
4. Controller action: `[RequiresPermission("<key>")]`, `[EndpointSummary("Türkçe özet")]`;
   reads → service, writes → `bus.InvokeAsync<T>(...)`.
5. Add the permission (English key, **Turkish** title) to the module's `Permissions`, then
   `make permissions-scan` (+ `make permissions-sync` when Keycloak is up).
6. **Tests are mandatory**: integration cases (mirror `SettlementApiTests`) + unit
   validator/mapping tests; `make test` must stay green.

### Add a module
New project `Kentos.Modules.<Name>` referencing `Infrastructure`; implement
`IModule` (English slug, `LicenseKey`), call `AddModuleDbContext<TCtx>` in `Register`;
reference it from `Kentos.Host` and `Kentos.AdminCli`; add its slug to
`License:EnabledModules`.

---

## 13. Configuration keys

`ConnectionStrings:{Postgres,Mongo}`, `Mongo:Database`, `Keycloak:{Authority,
MetadataAddress,Audience,RequireHttpsMetadata,ServerUrl,Realm,ClientId}`,
`Audit:Provider`, `Geo:Srid`, `Cors:AllowedOrigins`, `License:EnabledModules`,
`OpenTelemetry:{OtlpEndpoint,ServiceName}`, `Serilog:*`. `.env.example` documents
the `__`-separated environment-variable form.

---

## 14. Gotchas

- `dotnet run --project X` uses the **launch dir** as content root; run the host
  from its own directory (or via launchSettings) so `appsettings.json` loads.
- After editing `appsettings.json`, a stale copy may linger in `bin/`; rebuild.
- Wolverine needs `WolverineFx.RuntimeCompilation` and
  `ServiceLocationPolicy.AlwaysAllowed`.
- PostGIS-on-PG18 image: `postgis/postgis:18-3.6` (mount at `/var/lib/postgresql`).
