# Using the API & its docs (strict guide)

The API is self-documenting via **OpenAPI** (`Microsoft.AspNetCore.OpenApi`) rendered
by **Scalar**. This page is the authoritative guide to discovering and calling
endpoints correctly.

## Where the docs live

| URL | What |
|---|---|
| `http://localhost:5080/docs` | **Docs home** — card grid of modules, each links to its Scalar UI |
| `http://localhost:5080/scalar` | Combined Scalar UI (all modules) |
| `http://localhost:5080/scalar/{slug}` | Per-module Scalar UI (e.g. `/scalar/hesap`, `/scalar/settlement`) |
| `http://localhost:5080/openapi/v1.json` | Combined OpenAPI document (all modules) |
| `http://localhost:5080/openapi/{slug}.json` | Per-module OpenAPI document (only that module's routes) |
| `http://localhost:5080/api/v1/metadata` | licensed modules (slug, name, version, icon) + permissions |
| `http://localhost:5080/health/live`, `/health/ready` | health probes |
| `http://localhost:5080/metrics` | Prometheus metrics |

Run with `make run` (host listens on `http://localhost:5080`); start at **`/docs`**.

## Per-module vs combined docs

Each enabled module has its **own** OpenAPI document and Scalar UI, plus a combined view:

- **`/docs`** is the entry point: a responsive, mobile-friendly card grid (`DocsHomePage`)
  showing each module's `Icon` + `DisplayName` + version. A card opens that module's
  Scalar at `/scalar/{slug}`; a leading **"Tüm Modüller"** card opens the combined
  `/scalar`.
- **Per-module document** (`/openapi/{slug}.json`) is scoped to that module's
  `/api/v*/{slug}/*` routes via `OpenApiOptions.ShouldInclude`. Its sidebar is the
  default **controller → method** list.
- **Combined document** (`/openapi/v1.json`) contains every module. Tagging is the
  **default controller-based** list (e.g. `Users`, `Roles`, `Provinces`) — there is **no**
  custom `x-tagGroups` module grouping, since the per-module documents already separate
  modules.

The route→module filter (parsed by `ModuleRoute`) only decides which document an endpoint
belongs to; tags come from the controller. New modules/resources appear automatically. The
per-module documents are registered after module discovery via `AddKentosModuleDocs` in
`Program.cs`.

## Routing & versioning (mandatory shape)

Every endpoint is:

```
/api/v{version}/{module-slug}/{resource}
e.g. /api/v1/settlement/neighborhoods,  /api/v1/hesap/users
```

- URL-segment versioning (`Asp.Versioning`), default `v1`.
- Module slug = the module's `Slug` (e.g. `settlement`, `hesap`). A module's endpoints
  appear only when the module is licensed/loaded.

## Authorizing in Scalar (step by step)

The API uses **HTTP Bearer** (self-issued JWT). There is no OAuth login button.

1. Get a token: call `POST /api/v1/hesap/auth/login` (it's `[AllowAnonymous]`) with
   `{ "userName": "admin", "password": "Admin!234" }`. Copy `accessToken` from the
   response.
2. In Scalar, open **Authentication**, choose the **Bearer** scheme, and paste the
   token (no `Bearer ` prefix — Scalar adds it).
3. Calls now send `Authorization: Bearer <token>`. When it expires (default 15 min),
   call `POST /api/v1/hesap/auth/refresh` with your `refreshToken` and re-paste.

Raw curl equivalent:

```bash
TOKEN=$(curl -s -X POST http://localhost:5080/api/v1/hesap/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"userName":"admin","password":"Admin!234"}' | jq -r .accessToken)

curl -s http://localhost:5080/api/v1/settlement/provinces \
  -H "Authorization: Bearer $TOKEN"
```

## Reading an endpoint's required permission

Every guarded action carries `[RequiresPermission("<key>")]`. This surfaces in OpenAPI
as the **`x-required-permission`** extension and is prepended to the operation
description (`**Permission:** \`<key>\``) — visible in Scalar. If your token's roles
don't resolve to that key, you get **403**. (Tokens carry roles; the server maps
roles→permissions — see [auth.md](auth.md).)

```bash
# list every endpoint→permission binding from the live document:
curl -s http://localhost:5080/openapi/v1.json \
  | jq -r '.paths | to_entries[] | .key as $p | .value | to_entries[]
           | "\(.key|ascii_upcase) \($p)\t\(.value["x-required-permission"] // "-")"'
```

## Lists & pagination (every list endpoint)

List endpoints take a `PagedRequest` from the query string and return a
`PagedResponse<T>`:

- Query: `?page=1&pageSize=20&sort=name&search=foo` (`pageSize` capped at 200).
- Response: `{ "items": [...], "totalCount", "page", "pageSize", "totalPages" }`.

## IDs

Public IDs are **UUIDv7** surfaced as `id`. The internal bigint `id` is never exposed.
Route ids are `{id:guid}`. Create responses return `201` with the new resource and a
`Location` header.

## Error format (RFC 7807 ProblemDetails)

All errors are `application/problem+json`:

```json
{
  "status": 422,
  "title": "İş kuralı ihlali",
  "type": "https://kentos.local/errors/business_rule",
  "detail": "…",
  "errorCode": "business_rule",
  "traceId": "…",
  "errors": { "name": ["Ad zorunludur."] }   // present for validation (400)
}
```

| Status | `errorCode` | When |
|---|---|---|
| 400 | `validation` | FluentValidation failed (`errors` map populated) |
| 401 | `unauthorized` | missing/invalid/expired token |
| 403 | `forbidden` | authenticated but lacks the required permission |
| 404 | `not_found` | resource missing |
| 409 | `conflict` / `concurrency` | uniqueness / optimistic concurrency |
| 422 | `business_rule` | domain rule violated |
| 500 | `internal` | unexpected — persisted to `denetim.hata_kayitlari` with the `traceId` |

Always log/quote the `traceId` when reporting a 5xx; it correlates to the stored
`ErrorLog` and the OpenTelemetry trace.

## Calling writes correctly

- Create/update bodies are JSON matching the command/request record (English camelCase
  keys). The id for updates comes from the **route**, not the body.
- Writes run through validation server-side; a 400 with an `errors` map means fix the
  body and retry. Don't retry a 422 without changing the request — it's a rule
  violation, not a transient error.
