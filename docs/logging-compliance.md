# Logging, audit & legal recording (KVKK / 5651)

This document describes **what is recorded, where, for how long, and how it is
protected** — and maps it to the Turkish obligations that typically apply. It is the
operational companion to [observability.md](observability.md) (metrics/traces).

> **Not legal advice.** Whether (and for how long) you must keep access/traffic logs
> depends on your provider class under **5651** (yer / erişim / içerik / toplu kullanım
> sağlayıcı) and on **KVKK (6698)** for personal data. Confirm scope, retention periods
> and timestamping duties with a bilişim hukuku / KVKK uzmanı. The stack below is built
> to *support* compliance; it is not automatically compliant.

## The three kinds of record

| Kind | Source | Stored where | Purpose |
|---|---|---|---|
| **HTTP access log** | nginx reverse proxy (JSON) | file `logs/nginx/access.log` → Loki | "who (IP) accessed what, when, result" — the 5651 erişim kaydı |
| **Application request log** | Serilog request logging (enriched) | Console + rolling file `logs/kentos-*.log` → Loki | app-side request record (method, path, status, client IP, user, traceId) |
| **Data-change audit** | Audit.NET EF interceptor | `denetim.denetim_kayitlari` (Postgres dev / Mongo prod) | who changed which entity, old/new values |

Plus error logs (`denetim.hata_kayitlari`), metrics (Prometheus) and traces (Jaeger) —
those are operational, not legal records.

## Pipeline

```
                 ┌──────────── nginx (:8080) ──────────┐
browser / Next ─▶│ JSON access log → logs/nginx/access  │─▶ proxy ─▶ API (host :5080)
                 └─────────────────────────────────────┘            │  Serilog → Console
                                                                     │          → logs/kentos-*.log
                              Promtail (tails both log dirs)         │  Audit.NET → denetim_kayitlari
                                       │                             │
                                       ▼                             ▼
                                     Loki  ◀── Grafana (Explore / dashboards)
```

- The reverse proxy sets `X-Forwarded-For`; the API runs `UseForwardedHeaders()` so the
  **real client IP** (not the proxy IP) is what gets logged and audited.
- **Promtail** ships both the nginx access log (`job="kentos-proxy"`) and the API file
  logs (`job="kentos-app"`) to **Loki**; query them in Grafana → Explore → **Loki**.

## What each request log contains

nginx access line (JSON): `time, remote_addr, x_forwarded_for, method, uri, status,
request_time, bytes_sent, user_agent, referer`.

Serilog request line is enriched (in `UseKentosRequestLogging`) with: `ClientIp`,
`UserName` (from the `preferred_username` claim, when authenticated), `UserAgent`,
`RequestHost`, `TraceId`, plus `RequestMethod/Path/StatusCode/Elapsed`. `TraceId`
correlates the log line with the OpenTelemetry trace and the persisted `ErrorLog`.

## Masking (KVKK)

The audit trail must never store secrets. `AuditExtensions.MaskSensitiveData` runs just
before each audit event is saved and replaces values of any column whose name contains
`password / parola / hash / secret / securitystamp / guvenlik_damgasi / concurrencystamp
/ eszamanlilik / token` with `***`. So e.g. `parola_hash`, `guvenlik_damgasi`, and the
refresh-token hash are never written to `denetim_kayitlari`.

> Client IP and user name are **kept** in access/audit logs on purpose — they are the
> point of a 5651 access record and a legitimate-interest/legal-obligation basis under
> KVKK. Apply additional masking only where a field is PII you are not obliged to keep.

## Retention

| Store | Mechanism | Default | Where to change |
|---|---|---|---|
| API file logs | Serilog `rollingInterval=Day`, `retainedFileCountLimit=31` | ~31 days | `appsettings.json` → Serilog → File |
| Loki (access + app logs) | compactor `retention_enabled` + `retention_period` | 720h (30d) | `ops/loki/loki-config.yml` |
| Audit DB (`denetim_kayitlari`) | **policy — not auto-purged yet** | keep per obligation | see below |

**Set the Loki/file retention to your legal obligation** (5651 saklama süresi). For the
audit DB, enforce retention with a scheduled purge (the project already has Quartz via
`AddKentosScheduling`): add a job that deletes/archives `denetim_kayitlari` older than the
required period. Until then, retention there is "keep forever" — fine for safety, but
review against KVKK data-minimisation.

## Integrity (5651)

5651 generally expects log **integrity** (kayıtların bütünlüğü). Not yet implemented and
deliberately left as an ops/legal decision:

- Ship Loki/file archives to **immutable storage** (e.g. S3/MinIO Object-Lock / WORM).
- Apply a **qualified timestamp** (nitelikli zaman damgası, TÜBİTAK/e-imza) and/or a hash
  chain over rotated log files periodically.
- Keep clocks correct via **NTP** and log in **UTC**.

## How to use it

```bash
make infra-up        # postgres, mongo, prometheus, grafana, jaeger, loki, promtail, nginx
make run             # API on 0.0.0.0:5080
# go through the proxy so access logs are produced:
curl -s http://localhost:8080/health/live
```

- **Through the proxy:** http://localhost:8080 (access-logged). Direct http://localhost:5080
  still works for local debugging but bypasses the proxy access log.
- **Read logs:** Grafana http://localhost:3001 → Explore → datasource **Loki** →
  `{job="kentos-proxy"}` (HTTP access) or `{job="kentos-app"}` (app logs). Example:
  `{job="kentos-proxy"} | json | status >= 400`.
- **Audit trail:** dev → Postgres `denetim.denetim_kayitlari`; prod → Mongo
  (`Audit:Provider=Mongo`).

## Production notes

- Put TLS, rate-limiting and IP allow-listing on the proxy; restrict
  `ForwardedHeaders` `KnownProxies`/`KnownIPNetworks` to the actual proxy (dev clears them
  to trust all).
- Run the API behind the proxy only (don't expose 5080 publicly) so every request is
  access-logged.
- Move Loki to object storage with WORM + retention matching your legal period; enable the
  audit-DB purge job; wire qualified timestamping if 5651 requires it for your class.
- File-log paths assume the dev run dir (`src/Kentos.Host/logs`); in production point
  Serilog/Promtail at the real deployment log path (or push directly to Loki).
