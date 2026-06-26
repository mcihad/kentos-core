---
name: create-permission
description: Add a permission to a Kentos module resource and bind it to the API endpoint(s) it guards — declares the key, places the [RequiresPermission] attribute on the controller action, regenerates permissions.json, and (if Keycloak is up) creates the client role. Use when asked to add a permission, authorize an endpoint, or restrict access to an action.
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
into a policy and `PermissionAuthorizationHandler` checks the JWT `permissions`
claim. Missing → 403; no token → 401. If the action does not exist yet, create the
use-case first (see **/create-resource** or **/update-resource**).

## 3. Regenerate the catalog & confirm it reached the endpoint

```bash
make build
make permissions-scan        # permissions.json now contains the new key
make run                     # then, in another shell:
curl -s http://localhost:5080/openapi/v1.json | grep -o "{module}.{resource}.{action}"
```
The key appearing in the OpenAPI document (as `x-required-permission` on that path)
confirms the binding.

## 4. Push to Keycloak (when Keycloak is running)

```bash
make permissions-sync        # creates the client role on kentos-client
```
Assign it to a user/group in Keycloak (or it is already covered if you re-run
`make provision`, which puts every permission on the `kentos-administrators` group).
A token for that user then carries the key in its `permissions` claim and the
endpoint returns 200.

## 5. Verify end-to-end (optional, Keycloak up)

```bash
TOKEN=$(curl -s -d grant_type=password -d client_id=kentos-client \
  -d username=admin@kentos -d 'password=Admin!234' \
  http://localhost:8080/realms/kentos/protocol/openid-connect/token | jq -r .access_token)
curl -s -o /dev/null -w "%{http_code}\n" -H "Authorization: Bearer $TOKEN" \
  -X POST http://localhost:5080/api/v1/{module}/{resources}/<id>/approve   # expect the action's success code
```

## Gotchas

- The permission only takes effect where its key is in a `[RequiresPermission]`
  attribute. Declaring the key without placing the attribute does nothing.
- `permissions.json` is generated from code (`make permissions-scan`); never edit it
  by hand.
- New client roles exist in Keycloak only after `make permissions-sync`; a token
  minted before the sync won't contain the new key.
