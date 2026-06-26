# Observability cheat-sheet (PromQL & LogQL)

Copy-paste queries for **this** project's metrics and logs. Background:
[observability.md](observability.md) · [logging-compliance.md](logging-compliance.md).

## Where to run them

- **Grafana → Explore** (pusula ikonu, http://localhost:3001): pick **Prometheus** for
  PromQL or **Loki** for LogQL. The *Metrics/Label browser* lists everything — use it to
  confirm a name instead of guessing.
- **Prometheus UI** (http://localhost:9090) → *Graph* tab for PromQL only.
- Generate traffic first: `for i in $(seq 1 50); do curl -s localhost:8080/health/live >/dev/null; done`

## Which tool for which question

| Soru | Araç |
|---|---|
| Ne kadar / ne kadar hızlı? (oran, p95, hata %) | **Prometheus** (PromQL) |
| Tam olarak ne oldu / kim erişti? (satır satır) | **Loki** (LogQL) |
| Bu *tek* istek nerede yavaşladı? | **Jaeger** (http://localhost:16686) |

---

## PromQL — yapı taşları

```promql
metric{label="x"}                         # filtrele
rate(metric_total[1m])                     # sayaç (counter) → saniyedeki artış
sum(rate(...))                             # tüm serileri topla
sum by (label) (rate(...))                 # bir etikete göre grupla
histogram_quantile(0.95, sum by (le) (rate(metric_bucket[5m])))   # p95
```
Kural: **counter** (`_count`, `_total`) → her zaman `rate(...[aralık])` ile sar. Histogram
yüzdelikleri `_bucket` + `le` etiketi ile.

## PromQL — hazır sorgular (HTTP)

```promql
# İstek hızı (req/s)
sum(rate(http_server_request_duration_seconds_count[1m]))

# Route'a göre istek hızı
sum by (http_route) (rate(http_server_request_duration_seconds_count[1m]))

# p95 / p99 gecikme (saniye)
histogram_quantile(0.95, sum by (le) (rate(http_server_request_duration_seconds_bucket[5m])))
histogram_quantile(0.99, sum by (le) (rate(http_server_request_duration_seconds_bucket[5m])))

# Route bazında p95 (en yavaş uçları bul)
histogram_quantile(0.95, sum by (le, http_route) (rate(http_server_request_duration_seconds_bucket[5m])))

# 5xx hata hızı
sum(rate(http_server_request_duration_seconds_count{http_response_status_code=~"5.."}[1m]))

# Hata oranı (% — 5xx / tüm istekler)
100 * sum(rate(http_server_request_duration_seconds_count{http_response_status_code=~"5.."}[5m]))
    / sum(rate(http_server_request_duration_seconds_count[5m]))

# Duruma göre dağılım
sum by (http_response_status_code) (rate(http_server_request_duration_seconds_count[5m]))

# Giden HTTP çağrıları (HttpClient)
sum(rate(http_client_request_duration_seconds_count[1m]))
```

## PromQL — hazır sorgular (çalışma zamanı / sağlık)

```promql
up{job="kentos"}                                   # 1 = scrape çalışıyor
scrape_duration_seconds{job="kentos"}              # scrape ne kadar sürdü

# .NET bellek / GC / thread pool (process_runtime_dotnet_* ailesi)
sum(process_runtime_dotnet_gc_heap_size_bytes)
rate(process_runtime_dotnet_gc_collections_count[5m])
process_runtime_dotnet_thread_pool_threads_count
```
> Tam adı bilmiyorsan: Explore → Prometheus → **Metric browser** → `process_runtime_dotnet`
> yazıp listele. (İsimler OTel exporter sürümüne göre küçük farklar gösterebilir.)

---

## LogQL — yapı taşları

```logql
{job="kentos-proxy"}                       # stream seç (zorunlu: en az bir etiket)
{job="kentos-proxy"} |= "error"            # satır içi metin filtresi
{job="kentos-proxy"} | json                # JSON satırını alanlara ayır
... | json | status >= 400                 # ayrılan alanla filtrele
sum(rate({job="kentos-proxy"}[1m]))        # log satır hızı (metriğe çevir)
```

## LogQL — proxy access log (`kentos-proxy`, nginx JSON)

Alanlar: `time, remote_addr, x_forwarded_for, method, uri, status, request_time, user_agent`.

```logql
# Tüm erişim kayıtları
{job="kentos-proxy"}

# Sadece hatalı yanıtlar (4xx/5xx)
{job="kentos-proxy"} | json | status >= 400

# Belirli bir yol
{job="kentos-proxy"} | json | uri =~ "/api/v1/hesap/.*"

# Belirli bir IP (5651 sorgusu: "şu IP ne zaman neye erişti")
{job="kentos-proxy"} | json | remote_addr = "172.19.0.1"

# Yavaş istekler (>0.5 sn)
{job="kentos-proxy"} | json | request_time > 0.5

# 5xx hız (saniyede)
sum(rate({job="kentos-proxy"} | json | status >= 500 [1m]))

# Duruma göre dağılım
sum by (status) (count_over_time({job="kentos-proxy"} | json [5m]))
```

## LogQL — uygulama logu (`kentos-app`, Serilog JSON)

Serilog JSON: üst alanlar `Timestamp, Level, MessageTemplate`; zenginleştirmeler
`Properties` altında → `| json` bunları `Properties_*` olarak düzleştirir.

```logql
# Tüm uygulama logları
{job="kentos-app"}

# Sadece hatalar
{job="kentos-app"} | json | Level = "Error"

# Bir kullanıcının istekleri
{job="kentos-app"} | json | Properties_UserName = "admin"

# Bir trace id (log ↔ trace ↔ ErrorLog korelasyonu)
{job="kentos-app"} | json | Properties_TraceId = "<traceId>"

# 4xx+ yanıtlar
{job="kentos-app"} | json | Properties_StatusCode >= 400
```
> Alan adlarını Explore → Loki'de `| json` sonrası açılan tabloda birebir doğrula
> (formatter'a göre `Properties_StatusCode` vs `StatusCode` değişebilir).

---

## İpuçları

- **Zaman aralığı** sağ üstten; çoğu `rate` sorgusu en az birkaç dakikalık veri ister.
- PromQL'de **instant** (tek değer) vs **range** (grafik) farkı: panelde range, stat'ta instant.
- Boş panel? Önce `up{job="kentos"}` = 1 mi (scrape çalışıyor mu), sonra trafik var mı, sonra
  metrik/alan adı doğru mu (Explore browser ile kontrol).
- Bir paneli beğendin → Grafana'da **Share → Export → Save to file** ile
  `ops/grafana/dashboards/` içine koy, kalıcı olsun.
