# Kentos Core — Architecture Contract (agents.md)

This file is the **authoritative architecture contract**. Any human or AI agent
working in this repository must follow it so the codebase stays consistent — **so
strictly that it should not matter which agent writes the code.** When in doubt,
mirror the reference module `src/Modules/Kentos.Modules.Settlement/` (resource CRUD)
and `src/Modules/Kentos.Modules.Hesap/` (auth + identity). Deviations must be
justified in code comments and reflected here.

Kentos Core is a **JWT-secured, permission-driven, modular monolith** built with
**.NET 10**. Authentication is self-issued (ASP.NET Core Identity lives in the core
**Hesap** module); there is **no external identity provider**. Modules are loaded
dynamically by license; every module owns its own `DbContext`, Postgres schema,
routes and permission set. All cross-cutting concerns (auth, audit, soft-delete,
telemetry, logging, exceptions, mapping, validation, CQRS, scheduling, pagination,
API docs) live in the shared infrastructure layer.

> **Section 15 is a hard compliance checklist.** A resource/module is "done" only
> when every box applies. Reviewers reject PRs that skip boxes.

---

## 1. The ONE naming rule (read this first)

**All code is English. Turkish appears ONLY as physical database object names
(schema / table / column), DB comments, and user-facing text.**

| Concern | Language | Example |
|---|---|---|
| Namespaces, classes, entities, **fields**, methods, variables, filter names, code comments | **English** | `Neighborhood { Name, DistrictId }` |
| DTO field names → JSON keys | **English** camelCase | `{ "name": "...", "districtId": "..." }` |
| Routes, slugs, permission keys, config keys | **English** | `settlement.neighborhood.create` |
| DB schema / table / column names | **Turkish** (via `HasDefaultSchema` / `ToTable` / `HasColumnName` only) | `yerlesim.mahalleler { ad, ilce_id }` |
| **DB comments** (`HasComment(...)`) | **Turkish** | `"Mahalle adı"` |
| **User-facing text** (FluentValidation messages, `[EndpointSummary]`, permission `title`, module `DisplayName`, user-facing exception labels) | **Turkish** | `"Ad zorunludur."`, `"Mahalle ekle"` |

- Tables are **plural**, snake_case, Turkish: `iller`, `ilceler`, `mahalleler`,
  `kullanicilar`, `roller`, `yetkiler`.
- Every table and every column **must** have `.HasComment(...)` (Turkish).
- The English↔Turkish bridge for each entity lives **only** in its
  `IEntityTypeConfiguration`.
- Permission keys are `module.resource.action`; standard actions are
  `list, view, create, update, delete` (see `PermissionAction`). Extra verbs
  (e.g. `assignroles`, `assignpermissions`) are allowed as explicit strings.

---

## 2. Stack & versions

- **.NET 10** (`global.json` pins SDK; `net10.0`). Solution: `Kentos.slnx`.
- **PostgreSQL 18 + PostGIS** (`uuidv7()` DB default; `Guid.CreateVersion7()` app-side).
- EF Core 10 + `Npgsql.EntityFrameworkCore.PostgreSQL` (+ NetTopologySuite),
  `EFCore.NamingConventions` (snake_case fallback).
- **ASP.NET Core Identity** (in the Hesap module) for users/roles/password hashing.
- **JWT Bearer** (`Microsoft.AspNetCore.Authentication.JwtBearer`) — self-issued,
  symmetric HMAC. Tokens carry **roles only**.
- **Wolverine** (CQRS mediator + messaging) — handlers are static `Handle` methods.
- **Mapster** (per-entity `IRegister`), **FluentValidation** (per command).
- **Audit.NET** (EF Core interceptor) → `AuditLog`; provider Postgres (dev) / Mongo (prod).
- **OpenTelemetry** + **Serilog**; **Scalar** + `Microsoft.AspNetCore.OpenApi`;
  **Asp.Versioning**; **Quartz**.
- Central Package Management (`Directory.Packages.props`); shared MSBuild
  (`Directory.Build.props`, nullable warnings are errors).

---

## 3. Solution layout

```
src/
  Kentos.SharedKernel/   BaseEntity, IEntity/IAuditable/ISoftDeletable, Result,
                         PagedRequest/PagedResponse, ICommand/IQuery,
                         RequiresPermissionAttribute, PermissionDefinition,
                         IPermissionResolver/IPermissionCacheInvalidator,
                         IModule, ModuleManifest, exceptions, ICurrentUser
  Kentos.Infrastructure/ EF base + interceptors, AuditLog/ErrorLog, Audit.NET,
                         GlobalExceptionHandler, JWT auth + permission policy +
                         DenyAllPermissionResolver, OpenTelemetry/Serilog,
                         Mapster/Quartz wiring, module loader, OpenAPI transformers,
                         DI extensions (AddKentos*)
  Kentos.Host/           Program.cs (dynamic module loading + HesapSeeder), MetadataController
  Modules/
    Kentos.Modules.Hesap/       CORE module: ASP.NET Identity (users/roles/permissions/
                                departments/groups/access-policies) + self-issued JWT auth
    Kentos.Modules.Settlement/  Reference resource module (provinces/districts/neighborhoods)
tools/Kentos.AdminCli/   Spectre.Console.Cli: permissions scan
tests/                   TestShared (Testcontainers + TestAuthHandler) + unit + integration
docs/                    Architecture + per-module technical docs (start at docs/README.md)
```

Modules **never** reference each other. They reference `SharedKernel` +
`Infrastructure` and communicate only via SharedKernel contracts or Wolverine
messages. Each module maps to its own Postgres schema.

### Canonical module directory tree (MANDATORY structure)

```
Kentos.Modules.<Name>/
  <Name>Module.cs                         IModule: Slug, DisplayName, Version, LicenseKey, Permissions, Register
  Domain/
    <Entity>.cs                           entity : BaseEntity (English)
    Configurations/<Entity>Configuration.cs   IEntityTypeConfiguration (Turkish bridge)
  Application/
    <Resource>/<Resource>Response.cs      response DTO(s) (English)
    <Resource>/List<Resource>s.cs         ListXsQuery : PagedRequest
    <Resource>/Create<Resource>.cs        command + validator + static handler (+ event on create)
    <Resource>/Update<Resource>.cs        command + validator + static handler
    <Resource>/<Op><Resource>.cs          other write ops as commands (assign, etc.)
  Services/
    I<Resource>Service.cs + <Resource>Service.cs   logic (DbContext + IMapper); methods take commands
  Mappings/<Resource>Mapping.cs           Mapster IRegister (Uuid→Id, FK→parent Uuid)
  Events/<Name>Events.cs                  domain event records
  Events/<Name>EventConsumers.cs          consumer classes ({Event}Handler, single Handle)
  Permissions/<Name>Permissions.cs        permission keys + PermissionDefinition list
  Infrastructure/
    <Name>DbContext.cs                    schema + ApplyConfigurations + base.OnModelCreating last
    <Name>DbContextDesignFactory.cs       IDesignTimeDbContextFactory (UseNpgsql + UseNetTopologySuite + snake_case)
    Migrations/                           EF migrations live in the module
  Api/<Resource>sController.cs            controller; reads→service, writes→bus
```

Folders that don't apply to a given module are omitted, but the **names above are
canonical** — do not invent alternates (e.g. don't use `Dtos/` instead of
`Application/`). Auth-specific concerns in Hesap add `Authentication/`,
`Authorization/`, `Access/`, `Startup/` — see `docs/modules/hesap.md`.

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
  - `AuditableEntityInterceptor`: **interface-based** — stamps `IAuditable`
    (`CreatedBy/At`, `UpdatedBy/At`), converts hard delete → soft delete for
    `ISoftDeletable`, and sets `Uuid` (`CreateVersion7`) + `Version++` for `BaseEntity`.
    Entities that cannot inherit `BaseEntity` (ASP.NET Identity users/roles)
    **implement `IAuditable` + `ISoftDeletable`** and get their `Uuid` from the
    `uuidv7()` DB default instead.
  - Audit.NET `AuditSaveChangesInterceptor`: records mutations to `AuditLog`.
- **Soft delete** uses an EF Core 10 **named query filter** `"SoftDelete"`, applied
  by `ModelBuilder.ApplySoftDeleteFilters()` (shared helper in
  `EntityConfigurationExtensions`); bypass with `IgnoreQueryFilters("SoftDelete")`.
- `EntityConfigurationExtensions.ConfigureBase<T>()` configures all base columns
  (Turkish names + comments). **Every `BaseEntity` config calls it.**
- **Join tables are hard-deleted** via `ExecuteDeleteAsync` (not soft delete) when
  replacing a set (role↔permission, group members, user↔department), so re-adding the
  same pair cannot collide with the unique index.

### Module DbContext
Derives from `KentosDbContext` (plain modules) **or** `IdentityDbContext<...>` (Hesap).
Sets `HasDefaultSchema("<turkish>")`, `HasPostgresExtension("postgis")` if geo,
`ApplyConfigurationsFromAssembly(...)`. A plain context calls `base.OnModelCreating`
**last** (it applies the soft-delete filter). An Identity context calls
`base.OnModelCreating` first (it maps Identity entities), then
`ApplyConfigurationsFromAssembly`, then `ApplySoftDeleteFilters()` **last**.
Register with `services.AddModuleDbContext<TContext>(configuration)`. The
design-time `IDesignTimeDbContextFactory` **must** mirror the runtime options
(`UseNpgsql(cs, n => n.UseNetTopologySuite()).UseSnakeCaseNamingConvention()`) or
migrations will report phantom pending model changes.

### Audit & error tables (`denetim` schema)
- `AuditLog` → `denetim.denetim_kayitlari`; `ErrorLog` → `denetim.hata_kayitlari`.
  They do **not** derive from `BaseEntity` and are excluded from auditing
  (no recursion). Held by `AuditingDbContext`.

---

## 5. Reads, writes, services & Wolverine (CQRS)

The role split — and the reason Wolverine is in the stack:

- **Services** (`Services/I<Resource>Service` + impl, registered scoped in the module's
  `Register`) hold all data/business logic (DbContext, Mapster, geometry). Service
  methods **take the command/query records** (not loose parameters) and return DTOs.
- **Reads (queries) bypass Wolverine.** The controller injects the service and calls it
  directly. List/query records are plain `[FromQuery]` request classes
  (`List<Resource>sQuery : PagedRequest`). Prefer Mapster `ProjectToType<TResponse>()`
  on the `IQueryable` so projection runs in SQL.
- **Writes (commands) go through Wolverine** (`bus.InvokeAsync<T>(command)`):
  1. Wolverine runs the FluentValidation middleware → `ValidationException` → 400.
  2. The static `Handle` calls the service, then for **creates publishes a domain
     event**: `await bus.PublishAsync(new <Resource>Created(...))`.
  3. A write with neither validation nor an event (typically `delete`) calls the
     service directly from the controller — don't route it through the bus for ceremony.
- **Domain events** are records (module-local, or in SharedKernel when cross-module).
  **Consumers** are **instance** classes named `{Event}Handler` with a single
  `Handle({Event} message, deps...)`. Wolverine routes the published event to the
  decoupled consumer — the producer never references it. This is the modular-monolith
  decoupling mechanism (read-model updates, cache invalidation, notifications,
  cross-module reactions). Discovery is by type-name suffix (`Handler`/`Consumer`) +
  a single `Handle` — avoid overloaded `Handle` methods in one class.
- Events are **in-process** today; for production reliability enable the Postgres
  transactional outbox so the event persists in the write's transaction.
- `MessagingExtensions.Configure` sets `ServiceLocationPolicy.AlwaysAllowed`,
  `UseFluentValidation(RegistrationBehavior.ExplicitRegistration)` (validators
  registered once per module via `AddValidatorsFromAssembly`), and
  `IncludeAssembly(moduleAssembly)` once. Runtime codegen needs
  `WolverineFx.RuntimeCompilation`.

One file per use-case (`CreateNeighborhood.cs` = command + validator + handler). DTOs in
`<Resource>Response.cs`; events in `Events/`. The route id for updates comes from the
**route**: the controller binds a body `Update<Resource>Request` (no id) and builds the
`Update<Resource>Command(id, ...)`.

---

## 6. Authentication & authorization (self-issued JWT)

There is **no Keycloak**. The Hesap module is the identity provider.

- **Tokens carry roles only** (claim `roles`) — never permissions. This keeps tokens
  small regardless of how many permissions exist (the original reason for the design).
- **Login/refresh/logout** live in `Hesap` (`/api/v1/hesap/auth/*`, `[AllowAnonymous]`).
  Access tokens are short-lived HMAC-SHA256 JWTs; refresh tokens are opaque, stored as
  a SHA-256 hash, and rotated on use. IP/time **access policies are enforced at login**.
- `[RequiresPermission("settlement.neighborhood.create")]` (subclass of
  `AuthorizeAttribute`) sets policy `perm:<key>`. `PermissionPolicyProvider`
  materializes a policy per `perm:` prefix.
- `PermissionAuthorizationHandler` reads the token's **roles**, resolves them to
  permission keys via `IPermissionResolver`, and checks the requirement. The Hesap
  `RolePermissionResolver` (singleton) caches the role→permission map from the DB and
  is invalidated via `IPermissionCacheInvalidator` on role/permission changes. The core
  registers a fail-closed `DenyAllPermissionResolver` (used if Hesap is absent).
  Missing permission → **403**; no/invalid token → **401**.
- **Permissions are system-defined.** `HesapSeeder` (run at startup from `Program.cs`)
  upserts every module's `IModule.Permissions` into `hesap.yetkiler` and bootstraps an
  admin role + user. Roles and role↔permission/user↔role assignments are user-managed CRUD.
- `AddKentosAuthentication` configures JwtBearer (`MapInboundClaims=false`,
  `RoleClaimType="roles"`, symmetric `Jwt:SigningKey`); `AddKentosAuthorization`
  registers the policy provider + handler + deny-all resolver.

See `docs/modules/hesap.md` and `docs/auth.md` for the full operation guide.

---

## 7. API surface

- Controllers (not minimal API). Route:
  `/api/v{version:apiVersion}/{module-slug}/{resource}` e.g.
  `/api/v1/settlement/neighborhoods`, `/api/v1/hesap/users`.
- `Asp.Versioning` URL-segment versioning, default `v1`.
- Lists return `PagedResponse<T>` from a `PagedRequest` (page/pageSize/sort/search);
  pagination is mandatory.
- Errors are **RFC7807 ProblemDetails** with `errorCode`, `traceId`, and `errors`
  (validation). `GlobalExceptionHandler` maps exceptions (`KentosException`
  subclasses: 401/403/404/409/422; `ValidationException`→400; otherwise 500) and
  persists 5xx as `ErrorLog` folded by fingerprint (`IErrorRecorder`).
- Each action has `[RequiresPermission(...)]` (except `[AllowAnonymous]` auth
  endpoints) and `[EndpointSummary("Türkçe özet")]`; add `[ProducesResponseType]`.
- OpenAPI: `PermissionOperationTransformer` adds `x-required-permission`;
  `BearerSecuritySchemeTransformer` adds the HTTP Bearer scheme; Scalar UI at `/scalar`.
- `/api/v1/metadata` lists licensed modules + permissions.
- `/metrics` (Prometheus), `/health/live`, `/health/ready`.

---

## 8. Modules & licensing

`IModule` (Slug, DisplayName, Version, LicenseKey, Permissions, Register). At startup
`ModuleLoader.DiscoverEnabledModules` loads `Kentos.Modules.*.dll`, keeps modules whose
`LicenseKey` is null (**core**, e.g. Hesap) or whose slug is in `License:EnabledModules`.
The host calls `module.Register(...)`, adds the module assembly as an MVC
`ApplicationPart`, registers `ModuleRegistry`, scans Mapster `IRegister`s, and includes
the assembly in Wolverine discovery.

---

## 9. Observability, scheduling, audit provider

- OpenTelemetry traces (AspNetCore/HttpClient/EF/Npgsql/Wolverine) + metrics
  (+ Prometheus), OTLP export when `OpenTelemetry:OtlpEndpoint` is set.
- Serilog via `UseKentosSerilog` + request logging.
- Quartz via `AddKentosScheduling`.
- **Audit provider** chosen by `Audit:Provider` (`Postgres` dev / `Mongo` prod);
  `AddKentosAudit` wires the writer; `AuditExtensions.ConfigureAuditNet(app.Services)`
  is called once after build. Every module DbContext is audited automatically because
  `AddModuleDbContext` attaches the Audit.NET interceptor.

---

## 10. AdminCli (`tools/Kentos.AdminCli`)

Spectre.Console.Cli. Command:
- `permissions scan -o permissions.json` — reflects every module's `Permissions` and
  writes the catalog (useful for diffing/CI). There is no external IdP to provision;
  permissions are seeded into the DB at startup by `HesapSeeder`.

`permissions.json` is generated from code. The permission key is the single source of
truth shared by the attribute, OpenAPI, and the seeded `hesap.yetkiler` catalog.

---

## 11. Tests

- `Kentos.TestShared`: `ApiFactory` (`WebApplicationFactory<Program>` + a PostGIS
  Testcontainer) and `TestAuthHandler`. The handler reads `X-Test-User` /
  `X-Test-Permissions`; each permission value is emitted as a **`roles`** claim, and
  the factory registers a passthrough `IPermissionResolver` (role == permission), so
  `CreateClientWith(perm...)` exercises authorization without issuing real tokens.
- **Critical:** under `WebApplicationFactory` + minimal hosting,
  `ConfigureAppConfiguration` does **not** override `appsettings.json`. Inject test
  config via **environment variables** in `InitializeAsync`
  (`ConnectionStrings__Postgres`, `OpenTelemetry__OtlpEndpoint=""`). `Jwt:SigningKey`
  comes from `appsettings.json`.
- Integration tests use `[Collection("api")]` with a local `[CollectionDefinition]`
  in the **same assembly** (xUnit requirement).
- **Tests are mandatory** for every resource: integration cases (mirror
  `SettlementApiTests`/`HesapRoleTests`) + unit validator/mapping tests. `make test`
  must stay green.

---

## 12. Recipes

Automated by the skills `/create-module`, `/create-resource`, `/update-resource`,
`/create-permission` (in `.claude/skills/`) and verified with `make` targets. Use
them; sections 4–7 + 15 are the underlying contract.

---

## 13. Configuration keys

`ConnectionStrings:{Postgres,Mongo}`, `Mongo:Database`,
`Jwt:{Issuer,Audience,SigningKey,AccessTokenMinutes,RefreshTokenDays}`,
`Hesap:Bootstrap:{UserName,Password,Email}`, `Audit:Provider`, `Geo:Srid`,
`Cors:AllowedOrigins`, `License:EnabledModules`,
`OpenTelemetry:{OtlpEndpoint,ServiceName}`, `Serilog:*`. `.env.example` documents the
`__`-separated environment-variable form. **`Jwt:SigningKey` must be overridden per
environment** (the committed value is dev-only).

---

## 14. Gotchas

- `dotnet run --project X` uses the **launch dir** as content root; run the host from
  its own directory so `appsettings.json` loads.
- After editing `appsettings.json`, a stale copy may linger in `bin/`; rebuild.
- Wolverine needs `WolverineFx.RuntimeCompilation` and
  `ServiceLocationPolicy.AlwaysAllowed`.
- The design-time DbContext factory must match the runtime EF options exactly (incl.
  `UseNetTopologySuite`), or `migrate` throws `PendingModelChangesWarning`.
- PostGIS-on-PG18 image: `postgis/postgis:18-3.6`.

---

## 15. Compliance checklist (HARD — a PR is not done until every applicable box is true)

### Per resource (entity + endpoints)
- [ ] `Domain/<Entity>.cs : BaseEntity` (English class/fields).
- [ ] `Domain/Configurations/<Entity>Configuration.cs`: `ToTable("<turkish_plural>", t => t.HasComment(...))`,
      `builder.ConfigureBase()`, `HasColumnName` + `HasComment` (Turkish) on **every** property, indexes, relations.
- [ ] `DbSet<Entity>` added to the module DbContext.
- [ ] `Application/<Resource>/<Resource>Response.cs` (English fields; `Id` ← `Uuid`).
- [ ] `Mappings/<Resource>Mapping.cs` — Mapster `IRegister` (`Id`←`Uuid`, FK→parent `Uuid`, counts).
      (Composed/polymorphic responses may be built in the service — document why in a comment.)
- [ ] `Application/<Resource>/List<Resource>s.cs` — `: PagedRequest` (read, no Wolverine).
- [ ] Reads in `I<Resource>Service` use `ProjectToType<TResponse>()`; controller calls the service directly.
- [ ] Each **write** has `Application/<Resource>/<Verb><Resource>.cs` = command (`ICommand<T>`) +
      `AbstractValidator` (**Turkish** messages) + static `Handle`. Create publishes
      `<Resource>Created`; a consumer `Events/...EventConsumers.cs` reacts. Delete with no
      validation/event calls the service directly.
- [ ] Controller: `[RequiresPermission("<key>")]` + `[EndpointSummary("Türkçe")]` +
      `[ProducesResponseType]`; reads → service, writes → `bus.InvokeAsync<T>(...)`.
- [ ] Permission keys (English) + titles (**Turkish**) added to `Permissions/<Name>Permissions.cs`.
- [ ] EF migration added in the module (`Infrastructure/Migrations`).
- [ ] Integration tests (auth: 401 anon / 403 no-perm / 200 with perm) **and** unit
      validator/mapping tests. `make test` green.

### Per module (new module)
- [ ] Canonical directory tree (§3); project references `Infrastructure` only.
- [ ] `IModule` implemented (English slug; `LicenseKey` null only for core infra like Hesap).
- [ ] `AddModuleDbContext<TCtx>` in `Register`; design-time factory mirrors runtime options.
- [ ] Wired into `Kentos.Host` + `Kentos.AdminCli` (ProjectReference) and, if licensed,
      `License:EnabledModules` in `appsettings.json` + `.env.example`.
- [ ] Technical doc added under `docs/modules/<slug>.md`.
