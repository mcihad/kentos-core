---
name: update-resource
description: Modify an existing Kentos resource — add, rename, or remove fields, change validation, or add an operation — keeping the entity, EF config, response DTO, Mapster mapping, validators, CQRS commands/handlers, controller, and an EF migration all in sync. Use when asked to update, change, extend, or alter a resource/entity.
---

# update-resource

Changes a resource that **already exists**, touching every layer it appears in so
nothing drifts. Follows [agents.md](../../../agents.md); the reference is the
Settlement module. For a brand-new resource use **/create-resource** instead.

> Naming: code identifiers English; DB names + DB comments (`HasComment`) Turkish;
> user-facing text (validation messages, endpoint summaries, permission titles) Turkish.

## 0. Inputs (ask if missing)

- **Module** + **Resource** (e.g. `settlement` / `Neighborhood`).
- **Change**: add field / rename field / remove field / tighten validation /
  add a new operation (e.g. add `update`).
- For an added field: English property + type, **Turkish column**, English comment,
  constraints, and whether it is exposed in the DTO and/or settable on create/update.

## 1. Locate the layers

The resource spans (read them first):
```
Domain/{Resource}.cs
Domain/Configurations/{Resource}Configuration.cs     (Turkish HasColumnName + HasComment)
Application/{ResourcePlural}/{Resource}Response.cs
Application/{ResourcePlural}/Create*.cs, Update*.cs, Get*.cs, List*.cs   (thin handlers + Turkish validators)
Services/{Resource}Service.cs                         (the actual logic lives here)
Mappings/{Resource}Mapping.cs
Api/{ResourcePlural}Controller.cs                     (Turkish [EndpointSummary])
Permissions/{Module}Permissions.cs                   (Turkish titles)
tests/Kentos.Modules.{Module}.IntegrationTests + .UnitTests
```
Business/data changes go in the **service**; the handler stays a one-line delegator.

## 2. Apply the change consistently

**Add a field `Foo` (type `T`, column `foo`):**
1. Entity: add `public T Foo { get; set; }`.
2. Config: `builder.Property(e => e.Foo).HasColumnName("foo").HasComment("...");`
   (+ length/index/relationship as needed).
3. DTO: add `Foo` to `{Resource}Response` if it should be returned.
4. Mapping: add a `.Map(...)` only if the names differ or a transform is needed
   (e.g. geometry → lat/lng, or FK → parent `Uuid`).
5. Commands: add the parameter to `Create{Resource}Command` / `Update{Resource}Command`
   and set it in the handler.
6. Validators: add `RuleFor(x => x.Foo)...`.

**Rename a field:** change the English property + its `HasColumnName` (if the Turkish
column should change too) across all layers. Use `migrations add` to capture the column
rename.

**Remove a field:** delete it from every layer above, then migrate.

**Add an operation (e.g. update):** add the command + validator + handler file, a
controller action with `[RequiresPermission(...)]` + `[EndpointSummary(...)]`, and the
permission (see **/create-permission**).

## 3. Migration

```bash
make build
make migrate-add NAME=Update{Resource}Xxx MODULE_PROJECT=src/Modules/Kentos.Modules.{Module} CONTEXT={Module}DbContext
```
Review the generated migration (correct table/column names, no accidental drops),
then `make migrate`.

## 4. Update tests (MANDATORY)

A change is not done until its tests are updated:
- **Integration** (`tests/Kentos.Modules.{Module}.IntegrationTests`): adjust the
  resource's cases for the new shape; add a case for any new operation
  (incl. its 401/403 paths).
- **Unit** (`tests/Kentos.Modules.{Module}.UnitTests`): add/adjust validator tests
  for new/changed rules and the Mapster mapping test for new fields.

## 5. Verify

```bash
make build
make test            # all green, including the updated/new tests
make permissions-scan
```
If you added/changed permissions and Keycloak is up: `make permissions-sync`.

## Gotchas

- Keep all six layers in sync — a field added to the entity but missing from the DTO
  or validator is the classic bug.
- A removed/renamed column needs a migration; never hand-edit the DB.
- If a list/get now needs a related entity for mapping, add `.Include(...)` in that
  query's handler.
