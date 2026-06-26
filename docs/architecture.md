# Architecture overview

Kentos Core is a **JWT-secured, permission-driven, modular monolith** on **.NET 10**.
One process hosts many modules; each module owns its Postgres schema, routes and
permissions and is loaded dynamically by license.

## Layers

```
Kentos.SharedKernel   contracts only (entities, interfaces, exceptions, CQRS markers,
                      authorization abstractions, pagination) — no infrastructure
Kentos.Infrastructure cross-cutting implementation (EF base, interceptors, auth, audit,
                      errors, telemetry, mapping, messaging, OpenAPI, DI extensions)
Kentos.Host           composition root: loads modules, wires the pipeline, seeds
Modules/*             vertical slices (Domain, Application, Services, Api, ...)
```

Modules reference `SharedKernel` + `Infrastructure` only. **Modules never reference
each other** — they talk via SharedKernel contracts or Wolverine messages.

## Request lifecycle (write)

```
HTTP POST /api/v1/<module>/<resource>
  → Controller action ([RequiresPermission], [EndpointSummary])
  → Authentication (JWT bearer validates the self-issued token; roles claim)
  → Authorization (PermissionPolicyProvider → PermissionAuthorizationHandler →
                   IPermissionResolver resolves roles→permissions; 403 if missing)
  → bus.InvokeAsync<TResponse>(command)
      → Wolverine FluentValidation middleware (400 on invalid)
      → static Handle(command, service, bus): service does the work
      → bus.PublishAsync(<Resource>Created)  // create only
          → {Event}Handler consumer reacts (decoupled)
  → DbContext.SaveChanges
      → AuditableEntityInterceptor (audit fields, uuid, soft-delete conversion)
      → Audit.NET interceptor (writes AuditLog)
  → 201/200 with the response DTO
```

Reads skip Wolverine entirely: the controller calls the service, which projects to the
response DTO in SQL via Mapster `ProjectToType<T>()`.

## Cross-cutting concerns

| Concern | Where | Notes |
|---|---|---|
| Identity & permissions | Hesap module + `Infrastructure/Authorization` | tokens carry **roles only**; see [auth.md](auth.md) |
| Persistence base | `Infrastructure/Persistence/KentosDbContext`, `EntityConfigurationExtensions` | soft-delete filter, `ConfigureBase` Turkish columns |
| Audit fields & soft delete | `AuditableEntityInterceptor` (interface-based) | `IAuditable` + `ISoftDeletable`; hard delete → soft delete |
| Change auditing | Audit.NET interceptor → `denetim.denetim_kayitlari` | provider `Audit:Provider` = Postgres (dev) / Mongo (prod) |
| Errors | `GlobalExceptionHandler` | RFC7807; `KentosException` maps 401/403/404/409/422; 5xx persisted to `denetim.hata_kayitlari` via `IErrorRecorder` |
| Mapping | Mapster `IRegister` per resource | `Id`←`Uuid`; `ProjectToType` for lists |
| Messaging/CQRS | Wolverine | writes via `bus.InvokeAsync`; events via `bus.PublishAsync` |
| Telemetry | OpenTelemetry + Serilog | `/metrics`, OTLP when configured |
| Scheduling | Quartz (`AddKentosScheduling`) | |

## Module loading

`ModuleLoader.DiscoverEnabledModules` scans `Kentos.Modules.*.dll` for `IModule` and
keeps those whose `LicenseKey` is `null` (**core**, e.g. Hesap) or whose slug is listed
in `License:EnabledModules`. For each kept module the host:

1. calls `module.Register(services, config, env)` (DbContext, services, options),
2. adds the module assembly as an MVC `ApplicationPart` (controllers activate only when
   licensed),
3. registers `ModuleRegistry`, scans Mapster `IRegister`s, includes the assembly in
   Wolverine discovery.

## Startup sequence (`Program.cs`)

After `builder.Build()`, in order:

1. **EF migrations** — applied when `Database:MigrateOnStartup` is true (the default, all
   environments). Set it to `false` only if you apply migrations out-of-band; the schema
   must then already exist before startup.
2. **Seeding** — `HesapSeeder` (idempotent) runs *after* migrations, so the `hesap` schema
   is guaranteed to exist. It upserts every enabled module's `IModule.Permissions` into
   `hesap.yetkiler` and bootstraps the admin role + user.
3. **Wolverine message store** — created on startup via `AddResourceSetupOnStartup()`
   (schema `mesajlasma`).

## Messaging & the durable outbox

Writes go through Wolverine (`bus.InvokeAsync`) and publish domain events
(`bus.PublishAsync`). Wolverine uses a **durable Postgres outbox**
(`PersistMessagesWithPostgresql` + `Policies.AutoApplyTransactions()`): published events
are persisted, retried and dead-lettered, and survive a process restart — not in-memory
only. Consumers (`{Event}Handler`) react decoupled.

> Nuance: handlers here do their EF `SaveChanges` inside the service, then publish. The
> event is persisted to the outbox at the end of the handler's transaction, so delivery is
> **at-least-once and restart-safe**. For *strict* write↔event atomicity (one DB
> transaction spanning the entity write and the outbox), pass the `DbContext` into the
> handler and enable `UseEntityFrameworkCoreTransactions()`.

## Identity model (BaseEntity)

All domain entities derive from `BaseEntity` (`Id` bigint internal, `Uuid` public,
`Version`, `Metadata` jsonb, audit + soft-delete). The API exposes `Uuid` as `id`;
`Id` is never serialized. ASP.NET Identity entities (`ApplicationUser`,
`ApplicationRole`) can't derive from `BaseEntity`, so they implement `IAuditable` +
`ISoftDeletable` and carry a `Uuid` filled by the `uuidv7()` DB default.

See `agents.md` §15 for the per-resource/per-module compliance checklist every change
must satisfy.
