---
name: create-permission
description: Add a permission to a Kentos module resource and bind it to the API endpoint(s) it guards — declares the key, places the [RequiresPermission] attribute on the controller action, regenerates permissions.json; the permission is seeded into the DB (hesap.yetkiler) at startup and granted to roles via the Hesap API. Use when asked to add a permission, authorize an endpoint, or restrict access to an action.
---

# create-permission

Adds one permission and wires it to the endpoint where it applies. A permission is
"valid in an endpoint" precisely because its key sits in a
`[RequiresPermission("...")]` attribute on that controller action — which also makes
it appear in OpenAPI (`x-required-permission`) and Scalar. Follows
[agents.md](../../../agents.md). Reference: `SettlementPermissions` +
`NeighborhoodsController`.

> Permission keys are English: `module.resource.action` (e.g.
> `settlement.neighborhood.create`). Standard actions live in `PermissionAction`;
> custom verbs are allowed (e.g. `approve`, `export`).

## 0. Inputs (ask if missing)

- **Module** (slug, e.g. `settlement`) and **Resource** (e.g. `neighborhood`).
- **Action** — `list|view|create|update|delete|export` or a custom verb.
- **Endpoint(s)** — the controller action(s) this permission guards
  (e.g. `NeighborhoodsController.Create`). One permission may guard several actions.
- **Display title** — short English label, e.g. `"Approve neighborhood"`.

Key = `{module}.{resource}.{action}`.

## 1. Declare the key — `Permissions/{Module}Permissions.cs`

Add the constant to the resource's nested class (create the nested class if the
resource has none yet) and append a definition to `All`:

```csharp
public static class Neighborhood
{
    // ...existing...
    public const string Approve = "settlement.neighborhood.approve";
}

// in All:
Def("neighborhood", "approve", "Approve neighborhood"),
```

`Def` is the file's helper: `PermissionDefinition.Create(ModuleSlug, resource, action, title)`.
For a standard action use `PermissionAction.Create` etc. instead of a string literal.

## 2. Bind it to the endpoint — the controller action

On the guarded action add (or change) the attribute and keep a summary:

```csharp
[HttpPost("{id:guid}/approve")]
[RequiresPermission({Module}Permissions.Neighborhood.Approve)]
[EndpointSummary("Approve neighborhood")]
public async Task<IActionResult> Approve(Guid id, CancellationToken ct) { ... }
```

The attribute is what enforces it: `PermissionPolicyProvider` turns `perm:<key>`
into a policy and `PermissionAuthorizationHandler` reads the JWT's **`roles`** claim,
resolves them to permission keys via `IPermissionResolver` (the Hesap
`RolePermissionResolver`, DB-backed + cached), and checks the requirement. Missing →
403; no token → 401. **The token never carries permissions — only roles.** If the
action does not exist yet, create the use-case first (see **/create-resource** or
**/update-resource**).

## 3. Regenerate the catalog & confirm it reached the endpoint

```bash
make build
make permissions-scan        # permissions.json now contains the new key
make run                     # then, in another shell:
curl -s http://localhost:5080/openapi/v1.json | grep -o "{module}.{resource}.{action}"
```
The key appearing in the OpenAPI document (as `x-required-permission` on that path)
confirms the binding.

## 4. Seed it into the DB & grant it to a role

Permissions are **system-defined**: `HesapSeeder` upserts every module's declared
permissions into `hesap.yetkiler` on application startup. So just restart the app:

```bash
make run                     # HesapSeeder upserts the new key into hesap.yetkiler
```

Then grant the key to a role (user-managed) via the Hesap API — the `yonetici`
(admin) role automatically receives every permission at startup, so admins are
already covered. For another role:

```
PUT /api/v1/hesap/roles/{roleId}/permissions
{ "permissionKeys": ["settlement.neighborhood.approve", ...] }   # full replace
```

A user holding that role then gets a token carrying the **role** (not the
permission); the server resolves role→permission and the endpoint returns 200.
Role↔permission changes invalidate the resolver cache immediately (no re-login needed).

## 5. Verify end-to-end

```bash
# log in (bootstrap admin), then call the guarded endpoint with the access token
TOKEN=$(curl -s -X POST http://localhost:5080/api/v1/hesap/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"userName":"admin","password":"Admin!234"}' | jq -r .accessToken)
curl -s -o /dev/null -w "%{http_code}\n" -H "Authorization: Bearer $TOKEN" \
  -X POST http://localhost:5080/api/v1/{module}/{resources}/<id>/approve   # expect the action's success code
```

## Gotchas

- The permission only takes effect where its key is in a `[RequiresPermission]`
  attribute. Declaring the key without placing the attribute does nothing.
- `permissions.json` is generated from code (`make permissions-scan`); never edit it
  by hand.
- The permission row appears in `hesap.yetkiler` only after the app restarts
  (`HesapSeeder` runs at startup). A role must then be granted the key, and the user
  must hold that role.
