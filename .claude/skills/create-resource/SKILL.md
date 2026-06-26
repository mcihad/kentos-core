---
name: create-resource
description: Scaffold a complete resource in a Kentos module — entity, EF config (Turkish table/columns/comments), response DTO, Mapster mapping, FluentValidation (Turkish messages), application service + interface, Wolverine CQRS commands/queries + thin handlers, controller (Turkish summaries), permissions (Turkish titles), an EF migration, AND tests. Use when asked to add/create/scaffold a resource, entity, table, or CRUD endpoints in a module.
---

# create-resource

Adds a fully working, **tested** resource to an existing Kentos module, following
[agents.md](../../../agents.md). The canonical, already-tested example is the
**Neighborhood** resource in the Settlement module — read it first and mirror it:

- Entity `Domain/Neighborhood.cs`, Config `Domain/Configurations/NeighborhoodConfiguration.cs`
- Service `Services/NeighborhoodService.cs` (interface + impl — holds the logic)
- Use-cases `Application/Neighborhoods/*.cs` (records + validators + **thin** handlers)
- Mapping `Mappings/NeighborhoodMapping.cs`, Controller `Api/NeighborhoodsController.cs`
- Permissions `Permissions/SettlementPermissions.cs`
- Tests `tests/Kentos.Modules.Settlement.IntegrationTests/SettlementApiTests.cs`

> **Naming (agents.md §1):** code identifiers English; **DB names** (schema/table/
> column) Turkish via mapping strings; **DB comments** (`HasComment`) Turkish;
> **user-facing text** (validation messages, endpoint summaries, permission titles)
> Turkish.

## 0. Gather inputs (ask the user if not given)

Module (slug + dir), Resource (English singular `Product`, plural `products`,
**Turkish table** `urunler`), fields (English property + type, **Turkish column**,
**Turkish comment**, constraints), FK parents (referenced by parent **`Uuid`**),
operations, and whether it has geometry.

Placeholders: `{Resource}`/`{resources}`/`{ResourcePlural}` (Product/products/Products),
`{table}` (urunler), `{Module}`/`{module}` (Settlement/settlement), `{Ctx}`
(SettlementDbContext).

## 1. Entity — `Domain/{Resource}.cs`
`public sealed class {Resource} : BaseEntity` with English properties (+ `long ParentId`
and a `Parent?` nav for FKs). XML/code comments stay English.

## 2. EF config — `Domain/Configurations/{Resource}Configuration.cs`
```csharp
builder.ToTable("{table}", t => t.HasComment("Türkçe açıklama"));
builder.ConfigureBase();
builder.Property(e => e.Name).HasColumnName("ad").HasMaxLength(128).HasComment("Türkçe açıklama");
// FK: builder.Property(e => e.ParentId).HasColumnName("xxx_id").HasComment("...");
//     builder.HasOne(e => e.Parent).WithMany(...).HasForeignKey(e => e.ParentId).OnDelete(DeleteBehavior.Restrict);
```
Every column needs a Turkish `HasComment` and a Turkish `HasColumnName`.

## 3. Response DTO — `Application/{ResourcePlural}/{Resource}Response.cs`
Record with English fields; `Id` (Guid) ← entity `Uuid`; FK fields are the parent's `Uuid`.

## 4. Mapster mapping — `Mappings/{Resource}Mapping.cs`
`IRegister`; `.Map(d => d.Id, s => s.Uuid)`, FK `.Map(d => d.ParentId, s => s.Parent!.Uuid)`.

## 5. Service (the logic) — `Services/{Resource}Service.cs`
Interface + implementation holding the data/business logic (mirror
`NeighborhoodService`):
```csharp
public interface I{Resource}Service
{
    Task<{Resource}Response> CreateAsync(Create{Resource}Command command, CancellationToken ct);
    Task<{Resource}Response> GetByIdAsync(Guid id, CancellationToken ct);
    Task<PagedResponse<{Resource}Response>> ListAsync(List{Resource}Query query, CancellationToken ct);
    // + UpdateAsync / DeleteAsync as needed
}

public sealed class {Resource}Service({Ctx} db, IMapper mapper) : I{Resource}Service
{
    public async Task<{Resource}Response> CreateAsync(Create{Resource}Command command, CancellationToken ct)
    {
        var entity = new {Resource} { Name = command.Name };
        db.{ResourcePlural}.Add(entity);
        await db.SaveChangesAsync(ct);
        return mapper.Map<{Resource}Response>(entity);
    }
    // GetByIdAsync: throw NotFoundException.For("Türkçe ad", id);  // Turkish entity label
    // ListAsync: filter + paginate + map (see ProvinceService)
}
```
FK lookups resolve the parent by `Uuid` and set the navigation. Geometry uses
`GeometryParser` + `IOptions<GeoOptions>` (see `NeighborhoodService`).

## 6. Writes go through Wolverine; reads don't

**Reads** (get/list) need no command/handler — the controller calls the service directly.
List uses a plain `[FromQuery]` request class (`List{Resource}Query : PagedRequest`).

**Writes** — one file per operation in `Application/{ResourcePlural}/`: the command record
(`ICommand<T>`), an `AbstractValidator` with **Turkish** `.WithMessage(...)`, and a static
handler that calls the service and (for creates) publishes a domain event:
```csharp
public sealed record Create{Resource}Command(string Name) : ICommand<{Resource}Response>;

public sealed class Create{Resource}CommandValidator : AbstractValidator<Create{Resource}Command>
{
    public Create{Resource}CommandValidator() =>
        RuleFor(x => x.Name).NotEmpty().WithMessage("Ad zorunludur.")
            .MaximumLength(128).WithMessage("Ad en fazla 128 karakter olabilir.");
}

public static class Create{Resource}Handler
{
    public static async Task<{Resource}Response> Handle(
        Create{Resource}Command command, I{Resource}Service service, IMessageBus bus, CancellationToken ct)
    {
        var result = await service.CreateAsync(command, ct);
        await bus.PublishAsync(new {Resource}Created(result.Id, result.Name));
        return result;
    }
}
```
A write with no validation **and** no event (e.g. `delete`) skips the bus — the controller
calls the service directly.

## 6b. Domain event + consumer — `Events/`
```csharp
// SettlementEvents-style record:
public sealed record {Resource}Created(Guid Id, string Name);

// Consumer — INSTANCE class, name ends "Handler", a single Handle (Wolverine discovery):
public sealed class {Resource}CreatedHandler(ILogger<{Resource}CreatedHandler> logger)
{
    public void Handle({Resource}Created message) =>
        logger.LogInformation("[event] ... {Name}", message.Name);
}
```
The decoupled consumer is what makes Wolverine worth having (read-models, cache, cross-module).

## 7. Controller — `Api/{ResourcePlural}Controller.cs`
`(I{Resource}Service service, IMessageBus bus)`. Route
`/api/v{version:apiVersion}/{module}/{resources}`; each action
`[RequiresPermission({Module}Permissions.{Resource}.X)]` + `[EndpointSummary("Türkçe özet")]`.
**Reads** call `service.…`; **writes** call `bus.InvokeAsync<…>(command)`.

## 8. Permissions — `Permissions/{Module}Permissions.cs`
Nested `public static class {Resource}` with English `const` keys; append
`Def("{resource}", PermissionAction.X, "Türkçe başlık")` to `All`.

## 9. Register & migrate
- DbContext: `public DbSet<{Resource}> {ResourcePlural} => Set<{Resource}>();`
- Module `Register`: `services.AddScoped<I{Resource}Service, {Resource}Service>();`
```bash
make build
make migrate-add NAME=Add{Resource} MODULE_PROJECT=src/Modules/Kentos.Modules.{Module} CONTEXT={Ctx}
make migrate
```

## 10. Tests (MANDATORY — a resource is not done without them)
- **Integration** — add `{Resource}` cases to the module's
  `tests/Kentos.Modules.{Module}.IntegrationTests` (mirror `SettlementApiTests`):
  anonymous → 401, no-permission → 403, create+get with permission → 201/200,
  list pagination, (update/delete → soft-delete) if present. Use
  `_factory.CreateClientWith({Module}Permissions.{Resource}.Create, ...)`.
- **Unit** — validator tests (valid/invalid) and a Mapster mapping test in
  `tests/Kentos.Modules.{Module}.UnitTests` (mirror `ValidatorTests`/`MappingTests`).

## 11. Verify
```bash
make build
make test                 # ALL green, including the new resource's tests
make permissions-scan     # permissions.json includes the new keys
# Keycloak up: make permissions-sync
```

## Gotchas
- Logic lives in the **service**; handlers are one-line delegators (the CQRS entry
  point gets Wolverine validation middleware).
- Turkish only in: schema/table/column names, `HasComment`, validation messages,
  endpoint summaries, permission titles, user-facing exception labels. Everything
  else English.
- DTOs/commands reference parents by `Uuid` (Guid); the service resolves the `long` FK.
- A new module only loads if its slug is in `License:EnabledModules`.
