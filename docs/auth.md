# Authentication & Authorization (strict operation guide)

This is the definitive guide to how identity, tokens, roles, permissions and access
policies work in Kentos Core, and exactly how to operate them. **There is no Keycloak
or any external identity provider.** The core **Hesap** module is the identity
provider; the application issues and validates its own JWTs.

## The one rule that drives everything

> **The JWT carries roles only — never permissions.** Permission checks happen on the
> server by resolving the token's roles to permission keys.

Why: permissions can number in the thousands. Embedding them in the token bloats it
until requests fail. Roles are few, so a role-only token stays small, and the
server-side role→permission map (cached) does the fine-grained check.

## Components

| Piece | Location | Responsibility |
|---|---|---|
| `ApplicationUser` / `ApplicationRole` | `Hesap/Domain` | ASP.NET Identity entities (`long` keys, `Uuid`, audit/soft-delete) |
| `Permission` / `RolePermission` | `Hesap/Domain` | system-defined permission catalog (`hesap.yetkiler`) + role grants |
| `IJwtTokenService` / `JwtTokenService` | `Hesap/Authentication` | issues access (HMAC-SHA256, roles claim) + opaque refresh tokens |
| `IAuthService` / `AuthService` | `Hesap/Authentication` | login, refresh rotation, logout |
| `IAccessPolicyEvaluator` | `Hesap/Access` | login-time IP/time policy enforcement |
| `RolePermissionResolver` | `Hesap/Authorization` | `IPermissionResolver` + `IPermissionCacheInvalidator`; cached role→permission map |
| `HesapSeeder` | `Hesap/Startup` | upserts permission catalog + bootstraps admin role/user |
| `PermissionAuthorizationHandler` | `Infrastructure/Authorization` | reads `roles`, resolves, checks `[RequiresPermission]` |
| `DenyAllPermissionResolver` | `Infrastructure/Authorization` | fail-closed default if Hesap is absent |
| `AddKentosAuthentication/Authorization` | `Infrastructure/DependencyInjection` | JWT bearer validation + policy/handler/resolver wiring |

## Data model (`hesap` schema)

| Entity | Table | Purpose |
|---|---|---|
| `ApplicationUser` | `kullanicilar` | users |
| `ApplicationRole` | `roller` | roles (user-managed CRUD) |
| `Permission` | `yetkiler` | **system-defined** permissions (seeded at startup) |
| `RolePermission` | `rol_yetkileri` | role ↔ permission grants (user-managed) |
| `Department` | `departmanlar` | org tree (self-referencing) |
| `UserDepartment` | `kullanici_departmanlari` | user ↔ department |
| `UserGroup` / `UserGroupMember` | `kullanici_gruplari` / `kullanici_grup_uyeleri` | groups + membership |
| `AccessPolicy` | `erisim_politikalari` | IP/time allow-deny policies (per user or group) |
| `RefreshToken` | `yenileme_tokenlari` | rotated refresh tokens (SHA-256 hash stored) |

## Token lifecycle

1. **Login** — `POST /api/v1/hesap/auth/login` `{ "userName", "password" }`.
   `AuthService`: `UserManager.FindByName` + `CheckPasswordAsync`; on success it runs
   the **access-policy evaluation** (login-time only), loads the user's roles, and
   returns:
   ```json
   { "accessToken": "...", "accessTokenExpiresAt": "...",
     "refreshToken": "...", "refreshTokenExpiresAt": "..." }
   ```
   The access token is a JWT with `sub` (user `Uuid`), `preferred_username`, one
   `roles` claim per role, `jti`, `exp`. **No `permissions` claim exists.**
2. **Call the API** — send `Authorization: Bearer <accessToken>`.
3. **Refresh** — `POST /api/v1/hesap/auth/refresh` `{ "refreshToken" }`. The presented
   token is looked up by SHA-256 hash; if valid it is **rotated** (revoked + linked to
   its replacement) and a new pair is returned. A reused/old refresh token → 401.
4. **Logout** — `POST /api/v1/hesap/auth/logout` `{ "refreshToken" }` revokes it.

Config (`Jwt` section): `Issuer`, `Audience`, `SigningKey` (**override per
environment**), `AccessTokenMinutes` (15), `RefreshTokenDays` (14).

## How authorization actually decides

```
[RequiresPermission("settlement.neighborhood.create")]
  → policy "perm:settlement.neighborhood.create"
  → PermissionAuthorizationHandler:
        roles      = token.FindAll("roles")
        permissions = IPermissionResolver.ResolvePermissions(roles)   // cached map
        succeed iff permissions.Contains("settlement.neighborhood.create")
```

- No/invalid token → **401**. Authenticated but lacking the permission → **403**.
- `RolePermissionResolver` builds a `roleName → {permissionKeys}` snapshot from the DB
  on first use and caches it (volatile reference). Any role/permission change calls
  `IPermissionCacheInvalidator.Invalidate()` so the next request rebuilds it — **no
  re-login needed** for permission changes to take effect.
- If the Hesap module were absent, the core's `DenyAllPermissionResolver` denies
  everything (fail-closed).

## Permissions are system-defined

You never create permissions through the API. They are declared in code
(`<Module>Permissions.cs`, surfaced by `IModule.Permissions`) and **seeded into
`hesap.yetkiler` at startup** by `HesapSeeder` (idempotent upsert by key). To add one,
use the `/create-permission` skill, then restart the app.

Roles, and role↔permission grants, **are** user-managed.

## Operating it (recipes)

All management endpoints require the matching `hesap.*` permission; the bootstrap
`yonetici` (admin) role holds all of them.

```http
# 1) create a role
POST /api/v1/hesap/roles            { "name": "editor", "description": "İçerik editörü" }

# 2) grant permissions to the role (full replace)
PUT  /api/v1/hesap/roles/{roleId}/permissions
     { "permissionKeys": ["settlement.neighborhood.list", "settlement.neighborhood.create"] }

# 3) create a user
POST /api/v1/hesap/users
     { "userName": "ayse", "email": "ayse@x.com", "password": "Gizli!234", "roles": ["editor"] }

# 4) (re)assign a user's roles
PUT  /api/v1/hesap/users/{userId}/roles          { "roles": ["editor"] }

# 5) the user logs in and gets a token carrying role "editor"; the server resolves
#    editor → its permissions on every guarded request.
```

### Frontend bootstrap (`GET /api/v1/hesap/me`)

Any authenticated user can read their own context — use it right after login to drive
the UI (show/hide modules and elements):

```json
{
  "userId": "0192...", "userName": "ayse",
  "roles": ["editor"],
  "permissions": {
    "settlement": ["settlement.neighborhood.create", "settlement.neighborhood.list"],
    "hesap": ["hesap.user.list"]
  }
}
```

`permissions` is keyed by **module slug** → the full permission keys the user holds.
Show a module if its key exists in `permissions`; show an element if the relevant key
is in that module's array. The list reflects the live role→permission map, so it can
differ from an older token without re-login (re-fetch `/me` after role changes).

### Departments, groups, access policies

- Departments form a tree (`POST/PUT /api/v1/hesap/departments`, `parentId` optional);
  a department with children cannot be deleted.
- Groups (`/api/v1/hesap/groups`) have members (`.../{id}/members`).
- Access policies (`/api/v1/hesap/policies`) target a **user or group** and are
  **evaluated only at login**:
  - `kind = ip` → `value` is a CIDR (e.g. `10.0.0.0/8`); `kind = time` → `value` is a
    window `"SS:dd-SS:dd"` (24h, may wrap midnight).
  - `effect = deny` matching → login blocked. For a `kind` that has any `allow`
    policy, the request must match one (allow-list); kinds with no allow policy are
    unrestricted. Lower `priority` is evaluated first; deny wins.
  - Because policies are login-time only, a policy change affects new logins, not
    already-issued tokens (until they expire).

## Security checklist for deployment

- [ ] Override `Jwt:SigningKey` with a strong secret (≥ 32 bytes), per environment.
- [ ] Change the bootstrap admin password (`Hesap:Bootstrap:Password`) or disable the
      account after creating real admins.
- [ ] Keep `AccessTokenMinutes` short; rely on refresh rotation.
- [ ] Serve over HTTPS; tokens are bearer credentials.

## Testing auth

`TestAuthHandler` + `ApiFactory` bypass real tokens: `CreateClientWith("perm.key", …)`
emits each key as a `roles` claim, and a passthrough `IPermissionResolver` maps
role==permission. So tests assert authorization (401/403/200) without logging in. The
real login→token→resolve path is covered by `HesapAuthTests`/`HesapRoleTests`.
