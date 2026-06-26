# Module: Hesap (Account / Identity)

**Slug:** `hesap` · **Schema:** `hesap` · **License:** core (`LicenseKey = null`, always
loaded) · **Project:** `src/Modules/Kentos.Modules.Hesap`

Hesap is the identity provider: ASP.NET Core Identity (users, roles) extended with
permissions, departments, groups and access policies, plus self-issued JWT
authentication. For the auth *behaviour* and operation recipes see
[../auth.md](../auth.md); this page is the module's technical layout.

## Responsibilities

- Issue/validate the application's own JWTs (roles-only) — login, refresh, logout.
- Own the system-defined **permission catalog** and the role↔permission grants used by
  the global `PermissionAuthorizationHandler`.
- Manage users, roles, departments (tree), groups, and login-time access policies.
- Seed the permission catalog + bootstrap admin at startup (`HesapSeeder`).

## DbContext

`HesapDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, long, …>`
(`Infrastructure/HesapDbContext.cs`). It deviates from a plain module context because
it also is the Identity store:

```csharp
modelBuilder.HasDefaultSchema("hesap");
base.OnModelCreating(modelBuilder);                         // Identity entity mapping
modelBuilder.ApplyConfigurationsFromAssembly(typeof(HesapDbContext).Assembly);
modelBuilder.ApplySoftDeleteFilters();                      // MUST be last
```

Registered with `AddModuleDbContext<HesapDbContext>` (attaches the audit + Audit.NET
interceptors and the migration registration) **plus** `AddIdentityCore<ApplicationUser>
().AddRoles<ApplicationRole>().AddEntityFrameworkStores<HesapDbContext>()
.AddDefaultTokenProviders()`. `AddIdentityCore` (not `AddIdentity`) because auth is JWT,
not cookies. The design-time factory mirrors the runtime EF options (incl.
`UseNetTopologySuite`).

## Entities & tables (`hesap` schema)

| Entity | Table | Base | Notes |
|---|---|---|---|
| `ApplicationUser` | `kullanicilar` | `IdentityUser<long>` + `IAuditable`,`ISoftDeletable` | `Uuid` via `uuidv7()` default |
| `ApplicationRole` | `roller` | `IdentityRole<long>` + `IAuditable`,`ISoftDeletable` | `Description` |
| Identity joins | `kullanici_rolleri`, `kullanici_iddialari`, `rol_iddialari`, `kullanici_girisleri`, `kullanici_tokenlari` | — | Turkish table names |
| `Permission` | `yetkiler` | `BaseEntity` | unique `anahtar`; **seeded, not user-created** |
| `RolePermission` | `rol_yetkileri` | `BaseEntity` | unique `(rol_id, yetki_id)`; hard-deleted on reassign |
| `Department` | `departmanlar` | `BaseEntity` | self-FK `ust_departman_id` (tree, `Restrict`) |
| `UserDepartment` | `kullanici_departmanlari` | `BaseEntity` | user ↔ department |
| `UserGroup` / `UserGroupMember` | `kullanici_gruplari` / `kullanici_grup_uyeleri` | `BaseEntity` | groups + members |
| `AccessPolicy` | `erisim_politikalari` | `BaseEntity` | polymorphic subject (user/group), IP/time, allow/deny |
| `RefreshToken` | `yenileme_tokenlari` | `BaseEntity` | SHA-256 hash, rotated |

Identity entities can't extend `BaseEntity`; they implement `IAuditable` +
`ISoftDeletable` and get `Uuid` from the DB default. Their audit/soft-delete columns
are mapped by `IdentityAuditConfiguration` to the same Turkish names as `ConfigureBase`.

## Module-specific folders (beyond the canonical tree)

```
Authentication/   IJwtTokenService/JwtTokenService, IAuthService/AuthService, AuthContracts
Authorization/    RolePermissionResolver (IPermissionResolver + IPermissionCacheInvalidator)
Access/           IAccessPolicyEvaluator/AccessPolicyEvaluator (login-time IP/time)
Startup/          HesapSeeder (permission upsert + bootstrap admin)
```

These exist because identity/auth are genuine extra concerns; resource CRUD still
follows the standard `Application/Services/Mappings/Events/Api` layout (Users, Roles,
Departments, Groups, Policies, Permissions).

## Endpoints

| Area | Base route | Notes |
|---|---|---|
| Auth | `/api/v1/hesap/auth/{login,refresh,logout}` | `[AllowAnonymous]` |
| Me | `GET /api/v1/hesap/me` | `[Authorize]` — current user's roles + permissions grouped by module (UI bootstrap) |
| Users | `/api/v1/hesap/users` | + `/{id}/roles`, `/{id}/departments` |
| Roles | `/api/v1/hesap/roles` | + `/{id}/permissions` |
| Permissions | `/api/v1/hesap/permissions` | read-only catalog |
| Departments | `/api/v1/hesap/departments` | tree |
| Groups | `/api/v1/hesap/groups` | + `/{id}/members` |
| Policies | `/api/v1/hesap/policies` | login-time IP/time |

All non-auth endpoints are guarded by `hesap.*` permissions (`HesapPermissions`).

## CQRS specifics

Writes go through Wolverine with validators and publish events
(`UserCreated`, `UserRolesAssigned`, `RoleCreated`, `RolePermissionsAssigned`,
`DepartmentCreated`, `UserGroupCreated`, `AccessPolicyCreated`); consumers in
`Events/HesapEventConsumers.cs`. Reads use `ProjectToType<T>()`. **Login/refresh/logout
are not CQRS** — they're an auth protocol, handled directly by `AuthService`.
`AccessPolicy` responses are composed in the service (polymorphic subject → Uuid),
which is why it has no Mapster mapping.

## Startup seeding

`Program.cs` calls `HesapSeeder.SeedAsync` after migrations: upserts every enabled
module's `IModule.Permissions` into `yetkiler`, ensures the `yonetici` role holds all
permissions, and ensures the bootstrap admin user (`Hesap:Bootstrap:*`) exists in that
role. Idempotent — safe on every start.

## Configuration

`Jwt:{Issuer,Audience,SigningKey,AccessTokenMinutes,RefreshTokenDays}` and
`Hesap:Bootstrap:{UserName,Password,Email}`. **Override `Jwt:SigningKey` and the
bootstrap password per environment.**

## Tests

`tests/Kentos.Modules.Hesap.IntegrationTests` — `HesapAuthTests` (login returns
roles-only token, wrong password 401, refresh rotation, catalog seeded) and
`HesapRoleTests` (create role + assign permission read-back, Wolverine validation 400,
unknown-permission 422).
