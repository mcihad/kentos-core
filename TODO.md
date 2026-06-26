# Kentos Core — TODO / Backlog

Deferred work and known follow-ups, with enough context to pick up cold later. The
**authoritative rules** are in [`agents.md`](agents.md); this file is just the roadmap.
Ordered by priority. Each item: *problem → fix → files → effort → done-when*.

---

## P1 — Permission cache: correctness under load & scale

The role→permission map is cached in
`src/Modules/Kentos.Modules.Hesap/Authorization/RolePermissionResolver.cs`
(singleton, `volatile` snapshot, rebuilt by `Build()`, dropped by `Invalidate()`).
Two distinct problems:

### 1.1 Cache stampede (thundering herd) on rebuild
- **Problem:** `ResolvePermissions` does `var map = _snapshot ??= Build();`. Right after
  `Invalidate()` (snapshot = null), N concurrent requests each call `Build()` at the same
  time → N redundant DB round-trips (each opens a scope + queries `rol_yetkileri ⋈ yetkiler`).
  Wasteful and a latency spike under load. Single-instance issue.
- **Fix:** guard the rebuild with a `SemaphoreSlim(1,1)` + double-checked pattern: first
  thread builds, the rest await and reuse the freshly built snapshot. Keep the fast path
  (snapshot already present) lock-free.
  ```csharp
  private readonly SemaphoreSlim _rebuildLock = new(1, 1);

  public IReadOnlySet<string> ResolvePermissions(IEnumerable<string> roles)
  {
      var map = _snapshot ?? BuildSynchronized();
      ...
  }

  private IReadOnlyDictionary<string, HashSet<string>> BuildSynchronized()
  {
      _rebuildLock.Wait();
      try { return _snapshot ??= Build(); }   // double-check: someone may have built it
      finally { _rebuildLock.Release(); }
  }
  ```
  (Consider an async variant if `Build()` becomes async; today it's sync over a scoped DbContext.)
- **Files:** `RolePermissionResolver.cs`.
- **Effort:** ~30 min. Low risk.
- **Done when:** under concurrent first-hit-after-invalidate, `Build()` runs **once**
  (add a unit/integration test that invalidates then fires N parallel resolves and asserts a
  single DB build, e.g. via a build counter).

### 1.2 Distributed cache invalidation (multi-instance)
- **Problem:** `Invalidate()` only nulls the snapshot **in the current process**. With 2+
  app instances behind a load balancer, changing a role's permissions on instance A leaves
  B/C serving the **stale** map until they restart → inconsistent authorization. Depends on
  1.1 (otherwise every node stampedes right after a broadcast invalidation).
- **Fix:** broadcast invalidation across instances using **Postgres `LISTEN/NOTIFY`** (no new
  infra — Postgres is already there):
  - On role/permission change, after `SaveChanges`, `NOTIFY kentos_perm_invalidate`.
  - A hosted `BackgroundService` in Hesap opens a dedicated Npgsql connection, `LISTEN
    kentos_perm_invalidate`, and on notification calls `IPermissionCacheInvalidator.Invalidate()`
    locally. Each instance (including the originator) invalidates.
  - Keep the existing in-process `Invalidate()` call as the local trigger that also issues the
    NOTIFY (or route everything through the NOTIFY for a single path).
  - Add a safety **TTL** (e.g. rebuild if snapshot older than N minutes) so a missed
    notification can't pin a stale map forever.
- **Files:** `RolePermissionResolver.cs` (TTL), a new `PermissionCacheNotifier` (NOTIFY) +
  `PermissionCacheListener : BackgroundService` (LISTEN), wired in `HesapModule.Register`.
- **Effort:** ~half day. Medium risk (connection lifecycle, reconnect on drop).
- **Done when:** changing a role's permissions on one instance is reflected on another within
  ~1s without restart (manual multi-instance test or a documented procedure).

---

## P2 — Outbox: strict write↔event atomicity
- **Problem:** durable outbox is enabled (`PersistMessagesWithPostgresql` +
  `AutoApplyTransactions`, schema `mesajlasma`), so delivery is at-least-once and
  restart-safe. But handlers call `SaveChanges` inside the service and *then* publish, so the
  entity write and the outbox write are **not one DB transaction**; a crash in the small
  window between them can persist the entity without the event.
- **Fix:** enable `opts.UseEntityFrameworkCoreTransactions()` and let the handler's
  `DbContext` participate in Wolverine's transaction — typically by passing the module
  `DbContext` into the handler (or having the service defer `SaveChanges` to Wolverine).
  Needs a consistent pattern across modules; verify behavior with multiple module DbContexts.
- **Files:** `MessagingExtensions.cs`, handler signatures + services per module, `docs/architecture.md` (remove the "nuance" caveat once true).
- **Effort:** ~1 day (touches every write handler). Medium risk.
- **Done when:** an entity write and its domain event commit in a single transaction (kill the
  process between save and publish in a test → neither or both persist).

## P3 — Access policy enforcement & token revocation
- **Problem:** IP/time `AccessPolicy` is enforced **only at login**. An issued access token
  stays valid until expiry even if IP/time later violates a policy; `logout` only revokes the
  **refresh** token (access tokens are stateless, no denylist).
- **Fix (choose per requirement):**
  - Per-request policy check via middleware/authorization handler (re-evaluate IP/time) — adds
    a DB/cache lookup per request.
  - Short access-token lifetime (already 15 min) + optional access-token **denylist** (jti in a
    cache/Postgres) checked on auth for immediate revocation.
- **Files:** `Hesap/Access/*`, `Infrastructure/Authorization/*`, `AuthService`.
- **Effort:** ~half–1 day. **Done when:** a denied policy / logout blocks access within the
  chosen window.

## P4 — Audit retention purge (KVKK data minimisation)
- **Problem:** `denetim.denetim_kayitlari` grows unbounded; no purge. KVKK wants retention
  limits. (Loki + file logs already have retention; audit DB does not.)
- **Fix:** a Quartz job (`AddKentosScheduling` already present) that deletes/archives audit
  rows older than the legal retention period; make the period configurable.
- **Files:** new job under `Infrastructure` scheduling, config key `Audit:RetentionDays`,
  `docs/logging-compliance.md`. **Effort:** ~half day.

---

## P5 — Ops / security hardening (mostly config + small code)

- **Log integrity (5651):** ship Loki/file archives to **WORM / object-lock** storage; apply a
  **qualified timestamp (nitelikli zaman damgası)** and/or hash-chain over rotated logs; ensure
  **NTP** + UTC. (Ops + legal decision.) See `docs/logging-compliance.md`.
- **JWT / SSO:** current tokens are symmetric **HMAC** (signing key == validation key). For
  multi-service / SSO, move to **asymmetric RS256 + JWKS**, and add the **client-credentials**
  grant (service clients) — basis for the API-key "dynamic expose" architecture discussed
  (external callers → roles → existing `[RequiresPermission]`).
- **Rate limiting + output caching:** add .NET rate limiting (per API key / per IP) and Output
  Caching in `Kentos.Infrastructure` — important before exposing public APIs.
- **Health checks:** `/health/ready` only pings Postgres. Add Mongo (when audit provider =
  Mongo), Loki, and the Wolverine store.
- **Prod ForwardedHeaders:** dev clears `KnownProxies`/`KnownIPNetworks` (trust all). In prod,
  restrict to the actual reverse proxy; don't expose `:5080` publicly (serve via nginx only).

## P6 — Test coverage gaps
- `AccessPolicyEvaluator` (IP CIDR + time window incl. midnight wrap, allow-list vs deny).
- Refresh-token rotation edge cases (reuse after rotation, expiry, concurrent refresh).
- Permission cache invalidation behavior (ties into P1/P1.2).
- Optimistic concurrency conflicts (409) on updates.

---

### Notes
- Items P1.1 + P1.2 are best done together (invalidation without stampede protection just
  moves the herd). Recommended next pickup.
- Keep every change compliant with `agents.md` §15 (tests mandatory, build green).
