# Kentos Core — Documentation

Technical documentation for the Kentos Core modular monolith. The **authoritative
rules** live in [`agents.md`](../agents.md) (architecture contract). These docs
explain *how things work* and *how to operate them*; when the two disagree,
`agents.md` wins and should be corrected.

## Index

- [Architecture overview](architecture.md) — layers, request lifecycle, cross-cutting
  concerns (audit, soft-delete, errors, telemetry), how a module is loaded.
- [Authentication & authorization](auth.md) — the **strict** guide to how login,
  tokens, roles, permissions and access policies work and how to operate them.
- [Using the API docs (OpenAPI / Scalar)](api.md) — the **strict** guide to exploring
  and calling the API, authorizing in Scalar, versioning, paging and error format.
- [Observability (metrics, Prometheus, Grafana)](observability.md) — the metrics
  pipeline and how to actually use Prometheus & Grafana (incl. why they show "nothing").
- [Observability cheat-sheet (PromQL & LogQL)](observability-cheatsheet.md) — copy-paste
  queries for this project's metrics and logs.
- [Logging, audit & legal recording (KVKK / 5651)](logging-compliance.md) — access logs
  (nginx), request/audit logs, Loki/Promtail, masking, retention and integrity.
- Modules:
  - [Hesap (Account / identity)](modules/hesap.md) — core module: users, roles,
    permissions, departments, groups, access policies, JWT auth.
  - [Settlement (Yerleşim)](modules/settlement.md) — reference resource module.

## Quickstart

```bash
make up        # docker infra (postgres/mongo/otel) + apply migrations
make run       # run the API at http://localhost:5080 (Scalar at /scalar)
make test      # all tests (needs Docker for Testcontainers)
```

Default bootstrap admin (created idempotently at startup): `admin` / `Admin!234`.
Log in at `POST /api/v1/hesap/auth/login`.

## Conventions in one line

Code is English; **Turkish only** for DB schema/table/column names, `HasComment`
text, and user-facing strings (validation, endpoint summaries, permission titles).
See `agents.md` §1.
