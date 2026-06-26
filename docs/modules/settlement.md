# Module: Settlement (Yerleşim)

**Slug:** `settlement` · **Schema:** `yerlesim` · **License:** `settlement` (must be in
`License:EnabledModules`) · **Project:** `src/Modules/Kentos.Modules.Settlement`

Settlement is the **reference resource module** — the canonical example every other
resource module should mirror. It manages Turkish administrative geography: provinces
(`il`), districts (`ilçe`), neighborhoods (`mahalle`), including PostGIS geometry.

## Entities & tables (`yerlesim` schema)

| Entity | Table | Notes |
|---|---|---|
| `Province` | `iller` | `ad`, unique `plaka_kodu` (1..81) |
| `District` | `ilceler` | FK → province (`Restrict`) |
| `Neighborhood` | `mahalleler` | FK → district; PostGIS `Point` center + optional boundary |

All derive from `BaseEntity`; configs call `ConfigureBase()` and map every column to a
Turkish name with a Turkish `HasComment`. The DbContext enables
`HasPostgresExtension("postgis")` and calls `base.OnModelCreating` last.

## Layout (the pattern to copy)

```
Domain/{Province,District,Neighborhood}.cs + Configurations/*
Application/{Provinces,Districts,Neighborhoods}/
    {Resource}Response.cs            response DTO (Id ← Uuid)
    List{Resource}s.cs               : PagedRequest
    Create{Resource}.cs              command + validator + handler (publishes {Resource}Created)
    Update{Resource}.cs              (neighborhoods) command + validator + handler
Services/I{Resource}Service.cs + impl (DbContext + IMapper)
Mappings/{Resource}Mapping.cs        Mapster IRegister (Uuid→Id, FK→parent Uuid, geometry→lat/lng/WKT)
Events/SettlementEvents.cs + SettlementEventConsumers.cs
Permissions/SettlementPermissions.cs
Infrastructure/SettlementDbContext.cs + DesignFactory + Migrations/
Api/{Resource}sController.cs         reads→service, writes→bus
```

## Endpoints

`/api/v1/settlement/{provinces,districts,neighborhoods}` — list/get/create (and
update/delete on neighborhoods). Each action is guarded by a `settlement.*` permission
and has a Turkish `[EndpointSummary]`.

## Reads vs writes (the contract in action)

- **Reads** (`List`, `Get`): controller calls the service directly; the service uses
  Mapster to map loaded entities → DTOs.
- **Writes** (`Create`, `Update`): controller calls `bus.InvokeAsync<T>(command)`;
  Wolverine validates, the handler calls the service and (on create) publishes
  `ProvinceCreated` / `DistrictCreated` / `NeighborhoodCreated`; the matching
  `{Event}Handler` consumer reacts (here: logs).
- **Delete** (neighborhoods): no validation/event → controller calls the service
  directly (hard delete is converted to soft delete by the interceptor).

## Geometry

`Neighborhood` stores a `Point` center and optional polygon boundary (NetTopologySuite).
`NeighborhoodMapping` projects geometry to `Latitude`/`Longitude` and `BoundaryWkt`;
`GeometryParser` builds geometry from WKT on writes. `Geo:Srid` (default 4326).

## Tests

`tests/Kentos.Modules.Settlement.IntegrationTests` (`SettlementApiTests`: 401 anon,
403 no-permission, create+get, hierarchy + geometry) and
`Kentos.Modules.Settlement.UnitTests` (validators, Mapster config, pagination).

## Enabling/disabling

Settlement is licensed: include `settlement` in `License:EnabledModules`
(`appsettings.json` / `.env`). Removing it unloads the module — its controllers,
DbContext and permissions disappear from the running app (its permissions are also no
longer seeded into `hesap.yetkiler`).
