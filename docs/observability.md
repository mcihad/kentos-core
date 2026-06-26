# Observability (metrics, Prometheus, Grafana) — how it actually works

If Prometheus/Grafana show "nothing", it's almost always one of two setup gaps (both now
fixed in this repo): the **app wasn't being scraped**, or **Grafana had no datasource/
dashboard**. This page explains the pipeline and how to use each tool.

## The pipeline (who does what)

```
Kentos API (host, :5080)            Prometheus (container, :9090)        Grafana (container, :3001)
  OpenTelemetry metrics       →  scrapes GET /metrics every 15s   →  queries Prometheus (PromQL)
  exposed at /metrics            stores them as time series          and draws dashboards
```

- **App**: OpenTelemetry (`AddKentosObservability`) collects ASP.NET Core, HttpClient and
  .NET runtime metrics and exposes them in Prometheus format at **`/metrics`**
  (`app.MapPrometheusScrapingEndpoint()`).
- **Prometheus**: pulls (`scrapes`) `/metrics` on a timer and stores the numbers over
  time. Config: `ops/prometheus.yml` (target `host.docker.internal:5080`).
- **Grafana**: doesn't store anything; it **queries Prometheus** and visualizes. It needs
  a *datasource* (where Prometheus is) and *dashboards* (what to draw) — both
  auto-provisioned here from `ops/grafana/`.

## Start it

```bash
make infra-up      # postgres, mongo, prometheus, grafana, jaeger
make run           # API on 0.0.0.0:5080 (must stay running to be scraped)
# generate some traffic so HTTP metrics exist:
curl -s localhost:5080/health/live >/dev/null
```

| Tool | URL | Login |
|---|---|---|
| Prometheus | http://localhost:9090 | — |
| Grafana | http://localhost:3001 | admin / admin (anonymous viewing on) |
| App metrics (raw) | http://localhost:5080/metrics | — |

## Using Prometheus (the raw store)

1. **Targets** — http://localhost:9090/targets. The `kentos` job must be **UP**. If it's
   `down` with *connection refused*, the API isn't running or isn't bound to `0.0.0.0`
   (see [api.md] / the Makefile `BIND_URL`). This is the first thing to check.
2. **Query** — the *Graph* / *Query* tab. Type a **PromQL** expression and run it, e.g.:
   - `up{job="kentos"}` → 1 when scraping works.
   - `sum(rate(http_server_request_duration_seconds_count[1m]))` → requests/sec.
   - `histogram_quantile(0.95, sum by (le) (rate(http_server_request_duration_seconds_bucket[5m])))`
     → p95 latency (seconds).
   - `sum(rate(http_server_request_duration_seconds_count{http_response_status_code=~"5.."}[1m]))`
     → 5xx/sec.
   Prometheus is for ad-hoc queries and checking scrape health — not pretty dashboards.

## Using Grafana (the dashboards)

Grafana is auto-provisioned, so after `make infra-up`:

1. Open http://localhost:3001 → **Dashboards → Kentos → "Kentos – Genel Bakış"**. Panels:
   API up, request rate, 5xx rate, p95 latency, .NET GC heap. (Hit the API a few times so
   the HTTP panels fill; `up` and runtime panels show immediately.)
2. **Explore** (compass icon) → pick the **Prometheus** datasource → use the *Metrics
   browser* dropdown to see every available metric name and build a query without knowing
   PromQL by heart. This is the best way to discover what the app exposes.
3. Edit/add panels: each panel is a PromQL query against the Prometheus datasource.

### What's provisioned (and where to change it)

```
ops/grafana/provisioning/datasources/datasource.yml   Prometheus datasource (url http://prometheus:9090)
ops/grafana/provisioning/dashboards/provider.yml      loads dashboards from /var/lib/grafana/dashboards
ops/grafana/dashboards/kentos-overview.json           the starter dashboard
```

Add a dashboard by dropping its JSON into `ops/grafana/dashboards/` and restarting Grafana
(`docker compose restart grafana`). You can also build one in the UI and **Share → Export
→ Save to file** into that folder.

## Available metric families (from the current instrumentation)

- HTTP server: `http_server_request_duration_seconds_{count,sum,bucket}` (labels
  `http_request_method`, `http_response_status_code`, `http_route`).
- HTTP client: `http_client_request_duration_seconds_*`.
- .NET runtime: `process_runtime_dotnet_*` (GC, heap, thread pool, exceptions).
- Plus Prometheus' own `up`, `scrape_duration_seconds`, etc.

> Exact names depend on the OpenTelemetry exporter; if a query is empty, open Grafana
> **Explore → Metrics browser** to find the real name rather than guessing.

## Common "nothing shows" causes

| Symptom | Cause | Fix |
|---|---|---|
| Prometheus target `down`, *connection refused* | API not running, or bound to `127.0.0.1` only | `make run` (binds `0.0.0.0`); keep it running |
| Prometheus target `down` from a container | reaching host via `localhost` (= the container) | scrape `host.docker.internal:5080` (already set) |
| HTTP panels empty but `up`=1 | no traffic yet | call some endpoints; HTTP metrics are request-driven |
| Grafana totally empty | no datasource/dashboard | provisioned now; `make infra-up` (or `docker compose up -d grafana`) |
| A single panel empty | metric name differs | find the real name via Grafana **Explore → Metrics browser** |

## Traces (bonus)

Traces (not metrics) go to **Jaeger** when `OpenTelemetry:OtlpEndpoint` is set
(`http://localhost:4317`). Jaeger UI: http://localhost:16686 — search by service `kentos`
to see per-request spans (HTTP, EF/Npgsql, Wolverine). Metrics answer "how much/how fast";
traces answer "what happened in this one request".
