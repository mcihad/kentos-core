// Generates, per backend module: typed models (schema.ts), a permission-annotated SDK
// (sdk.ts), and TanStack Query hooks (queries.ts). Records what changed since the last
// run into frontend/TODO.md so the frontend never silently diverges from the server.
//
//   1. discover enabled modules from /api/v1/metadata (license-aware, same source of truth)
//   2. diff the new OpenAPI doc against the previously vendored openapi/<slug>.json
//   3. write openapi/<slug>.json (vendored, reproducible)
//   4. openapi-typescript → src/modules/<slug>/schema.ts     (types/models)
//   5. src/modules/<slug>/sdk.ts                             (named methods + @permission JSDoc)
//   6. src/modules/<slug>/queries.ts                         (TanStack Query hooks + @permission)
//   7. src/modules/<slug>/index.ts + src/modules/index.ts barrel
//   8. append changes (endpoints/methods/fields/removals) to frontend/TODO.md
//
// Usage:  pnpm gen   |   KENTOS_API_URL=https://api.example.com pnpm gen
import { execFileSync } from "node:child_process";
import { existsSync, mkdirSync, readFileSync, readdirSync, writeFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const todoPath = resolve(root, "..", "TODO.md");
const baseUrl = (process.env.KENTOS_API_URL ?? "http://localhost:5080").replace(/\/$/, "");
const HTTP_METHODS = ["get", "put", "post", "delete", "options", "head", "patch", "trace"];

const pascal = (s) => s.split(/[^a-z0-9]+/i).filter(Boolean).map((p) => p[0].toUpperCase() + p.slice(1)).join("");

async function getJson(path) {
  const res = await fetch(`${baseUrl}${path}`);
  if (!res.ok) throw new Error(`GET ${path} → ${res.status}`);
  return res.json();
}

/** "METHOD /path" → { method, path, permission, summary, requiredInit }. */
function operationsOf(spec) {
  const ops = new Map();
  for (const [path, item] of Object.entries(spec.paths ?? {})) {
    for (const method of HTTP_METHODS) {
      const op = item?.[method];
      if (!op) continue;
      ops.set(`${method.toUpperCase()} ${path}`, {
        method,
        path,
        permission: op["x-required-permission"] ?? null,
        summary: op.summary ?? null,
        requiredInit: Boolean(op.requestBody?.required) || (op.parameters ?? []).some((p) => p.required),
      });
    }
  }
  return ops;
}

function schemasOf(spec) {
  const out = new Map();
  for (const [name, schema] of Object.entries(spec.components?.schemas ?? {})) {
    const typeOf = new Map();
    for (const [prop, def] of Object.entries(schema.properties ?? {})) typeOf.set(prop, typeHint(def));
    out.set(name, { props: new Set(typeOf.keys()), typeOf });
  }
  return out;
}

function typeHint(def) {
  if (!def) return "any";
  if (def.$ref) return def.$ref.split("/").pop();
  if (def.type === "array") return `${typeHint(def.items)}[]`;
  const base = Array.isArray(def.type) ? def.type.join("|") : def.type;
  if (def.format) return `${base}<${def.format}>`;
  return base ?? (def.oneOf || def.anyOf || def.allOf ? "union" : "object");
}

/** Collection path used for cache invalidation: path up to the first {param}. */
function resourceBase(path) {
  const out = [];
  for (const seg of path.split("/")) {
    if (seg.startsWith("{")) break;
    out.push(seg);
  }
  return out.join("/") || path;
}

/** Builds a stable name + metadata for every operation; names are shared by sdk + queries. */
function buildMethods(slug, ops) {
  const used = new Set();
  return [...ops.values()]
    .sort((a, b) => `${a.path}${a.method}`.localeCompare(`${b.path}${b.method}`))
    .map((op) => {
      const segs = op.path.split("/").filter(Boolean);
      const i = segs.indexOf(slug);
      const rest = (i >= 0 ? segs.slice(i + 1) : segs.slice(3))
        .map((s) => (s.startsWith("{") ? `By${pascal(s.slice(1, -1))}` : pascal(s)));
      let name = op.method.toLowerCase() + rest.join("");
      let unique = name;
      for (let k = 2; used.has(unique); k++) unique = `${name}${k}`;
      used.add(unique);
      return {
        ...op,
        name: unique,
        isQuery: op.method === "get" || op.method === "head",
        initType: `FetchOptions<paths[${JSON.stringify(op.path)}][${JSON.stringify(op.method)}]>`,
        resourceBase: resourceBase(op.path),
      };
    });
}

function jsdoc(m, indent) {
  return [
    `${indent}/**`,
    `${indent} * ${m.summary ?? m.name}`,
    `${indent} * @route ${m.method.toUpperCase()} ${m.path}`,
    `${indent} * @permission ${m.permission ?? "(anonim — izin gerektirmez)"}`,
    `${indent} */`,
  ].join("\n");
}

function sdkFile(slug, methods) {
  const Pascal = pascal(slug);
  const body = methods
    .map((m) => {
      const param = m.requiredInit ? `init: ${m.initType}` : `init?: ${m.initType}`;
      return `${jsdoc(m, "    ")}\n    ${m.name}(${param}) {\n      return client.${m.method.toUpperCase()}(${JSON.stringify(m.path)}, init);\n    },`;
    })
    .join("\n");
  return `// AUTO-GENERATED by scripts/gen.mjs — do not edit. Regenerate with \`pnpm gen\`.
import type { Client, FetchOptions } from "openapi-fetch";
import type { paths } from "./schema";

/**
 * \`${slug}\` modülü SDK'sı. Her metodun üzerinde gerekli **permission** JSDoc olarak yazılıdır.
 * Parametre/gövde/yanıt tipleri \`schema.ts\`'ten gelir (tam tipli).
 */
export function create${Pascal}Sdk(client: Client<paths>) {
  return {
${body}
  };
}
`;
}

function queriesFile(slug, methods) {
  const Pascal = pascal(slug);
  const body = methods
    .map((m) => {
      const hook = `use${pascal(m.name)}`;
      const data = `Data<${JSON.stringify(m.name)}>`;
      if (m.isQuery) {
        const param = m.requiredInit ? `init: ${m.initType}` : `init?: ${m.initType}`;
        const opts = `options?: Omit<UseQueryOptions<${data}, Error>, "queryKey" | "queryFn">`;
        return `${jsdoc(m, "    ")}
    ${hook}(${param}, ${opts}) {
      return useQuery<${data}, Error>({
        queryKey: [${JSON.stringify(slug)}, ${JSON.stringify(m.path)}, init?.params ?? {}],
        queryFn: async () => {
          const { data, error } = await sdk.${m.name}(init);
          if (error) throw error as Error;
          return data as ${data};
        },
        ...options,
      });
    },`;
      }
      const opts = `options?: Omit<UseMutationOptions<${data}, Error, ${m.initType}>, "mutationFn">`;
      return `${jsdoc(m, "    ")}
    ${hook}(${opts}) {
      const queryClient = useQueryClient();
      return useMutation<${data}, Error, ${m.initType}>({
        mutationFn: async (init) => {
          const { data, error } = await sdk.${m.name}(init);
          if (error) throw error as Error;
          return data as ${data};
        },
        // Mutasyon sonrası ilgili kaynağın query'lerini tazele. options.onSuccess verirsen
        // bu davranışı sen üstlenirsin (aşağıdaki spread onu ezer).
        onSuccess: () => {
          void queryClient.invalidateQueries({
            predicate: (q) =>
              Array.isArray(q.queryKey) &&
              q.queryKey[0] === ${JSON.stringify(slug)} &&
              typeof q.queryKey[1] === "string" &&
              (q.queryKey[1] as string).startsWith(${JSON.stringify(m.resourceBase)}),
          });
        },
        ...options,
      });
    },`;
    })
    .join("\n");
  return `// AUTO-GENERATED by scripts/gen.mjs — do not edit. Regenerate with \`pnpm gen\`.
import { useMutation, useQuery, useQueryClient, type UseMutationOptions, type UseQueryOptions } from "@tanstack/react-query";
import type { FetchOptions } from "openapi-fetch";
import type { paths } from "./schema";
import { create${Pascal}Sdk } from "./sdk";

type Sdk = ReturnType<typeof create${Pascal}Sdk>;
type Data<M extends keyof Sdk> = NonNullable<Awaited<ReturnType<Sdk[M]>>["data"]>;

/**
 * \`${slug}\` modülü TanStack Query hook'ları (SDK üstünde cache + mutation yönetimi).
 * Her hook'un üzerinde gerekli **permission** JSDoc olarak yazılıdır. Bir kez \`sdk\` ile
 * kurun (web: configured client; RN: aynısı), dönen hook'ları bileşenlerde kullanın.
 */
export function create${Pascal}Queries(sdk: Sdk) {
  return {
${body}
  };
}
`;
}

function indexFile(slug) {
  const Pascal = pascal(slug);
  return `// AUTO-GENERATED by scripts/gen.mjs — do not edit. Regenerate with \`pnpm gen\`.
import type { paths } from "./schema";
import { createApiClient, type ApiClientOptions } from "../../client";
import { create${Pascal}Sdk } from "./sdk";

/** Ham tipli openapi-fetch client (\`${slug}\` modülü). */
export function create${Pascal}Client(options: ApiClientOptions) {
  return createApiClient<paths>(options);
}

/** Adlandırılmış, permission-yorumlu SDK. */
export function create${Pascal}Api(options: ApiClientOptions) {
  return create${Pascal}Sdk(create${Pascal}Client(options));
}

export type { paths, components, operations } from "./schema";
export * from "./sdk";
export * from "./queries";
`;
}

function diffModule(slug, oldSpec, newSpec) {
  const lines = [];
  const oldOps = oldSpec ? operationsOf(oldSpec) : new Map();
  const newOps = operationsOf(newSpec);
  const added = [...newOps.keys()].filter((k) => !oldOps.has(k));
  const removed = [...oldOps.keys()].filter((k) => !newOps.has(k));
  const permChanged = [...newOps.keys()].filter((k) => oldOps.has(k) && oldOps.get(k).permission !== newOps.get(k).permission);

  if (added.length) {
    lines.push("### Yeni endpoint / servis metodu");
    for (const k of added.sort()) {
      const o = newOps.get(k);
      lines.push(`- [ ] \`${k}\` — ${o.summary ?? ""} (permission: \`${o.permission ?? "anonim"}\`) → SDK + Query hook üretildi; UI/akış ekle`);
    }
  }
  if (permChanged.length) {
    lines.push("### Permission değişti");
    for (const k of permChanged.sort()) lines.push(`- [ ] \`${k}\`: \`${oldOps.get(k).permission ?? "anonim"}\` → \`${newOps.get(k).permission ?? "anonim"}\` → UI yetki kontrolünü güncelle`);
  }
  if (removed.length) {
    lines.push("### Kaldırılan endpoint");
    for (const k of removed.sort()) lines.push(`- [ ] \`${k}\` kaldırıldı → client/UI temizle`);
  }

  const oldSchemas = oldSpec ? schemasOf(oldSpec) : new Map();
  const newSchemas = schemasOf(newSpec);
  const fieldLines = [];
  for (const [name, s] of newSchemas) {
    const old = oldSchemas.get(name);
    if (!old) {
      if (oldSpec) fieldLines.push(`- [ ] Yeni tip \`${name}\` (${[...s.props].join(", ") || "alan yok"})`);
      continue;
    }
    for (const p of [...s.props].filter((p) => !old.props.has(p))) fieldLines.push(`- [ ] \`${name}\`: yeni alan \`${p}\` (${s.typeOf.get(p)})`);
    for (const p of [...old.props].filter((p) => !s.props.has(p))) fieldLines.push(`- [ ] \`${name}\`: kaldırılan alan \`${p}\``);
  }
  if (fieldLines.length) {
    lines.push("### Varlık / DTO alanları");
    lines.push(...fieldLines);
  }
  return lines;
}

console.log(`▸ API: ${baseUrl}`);
const manifests = await getJson("/api/v1/metadata");
const slugs = manifests.map((m) => m.slug).sort();
console.log(`▸ Modüller: ${slugs.join(", ") || "(yok)"}`);

mkdirSync(resolve(root, "openapi"), { recursive: true });
const todoSections = [];

for (const slug of slugs) {
  const specPath = resolve(root, "openapi", `${slug}.json`);
  const oldSpec = existsSync(specPath) ? JSON.parse(readFileSync(specPath, "utf8")) : null;
  const newSpec = await getJson(`/openapi/${slug}.json`);
  const methods = buildMethods(slug, operationsOf(newSpec));

  const moduleDir = resolve(root, "src", "modules", slug);
  mkdirSync(moduleDir, { recursive: true });
  writeFileSync(specPath, `${JSON.stringify(newSpec, null, 2)}\n`);
  execFileSync("npx", ["openapi-typescript", specPath, "-o", resolve(moduleDir, "schema.ts")], { stdio: "inherit", cwd: root });
  writeFileSync(resolve(moduleDir, "sdk.ts"), sdkFile(slug, methods));
  writeFileSync(resolve(moduleDir, "queries.ts"), queriesFile(slug, methods));
  writeFileSync(resolve(moduleDir, "index.ts"), indexFile(slug));
  console.log(`  ✓ ${slug} → src/modules/${slug}/ (${methods.length} operasyon)`);

  const diff = diffModule(slug, oldSpec, newSpec);
  if (diff.length) todoSections.push(`## ${slug}\n${diff.join("\n")}`);
}

writeFileSync(
  resolve(root, "src", "modules", "index.ts"),
  "// AUTO-GENERATED by scripts/gen.mjs — do not edit by hand.\n" +
    slugs.map((s) => `export * as ${s} from "./${s}/index";`).join("\n") + "\n",
);

if (todoSections.length) {
  const date = new Date().toISOString().slice(0, 10);
  const header = existsSync(todoPath)
    ? readFileSync(todoPath, "utf8").trimEnd()
    : "# Frontend TODO — backend ile senkron\n\n" +
      "> `pnpm gen` backend değişikliklerini buraya yazar. Frontend tarafında tamamlanan\n" +
      "> maddeleri işaretleyip bu bölümleri silin. Boşaldığında dosyayı temizleyebilirsiniz.";
  writeFileSync(todoPath, `${header}\n\n---\n\n# ${date}\n\n${todoSections.join("\n\n")}\n`);
  console.log(`▸ TODO güncellendi: frontend/TODO.md (${todoSections.length} modül değişikliği)`);
} else {
  console.log("▸ Değişiklik yok — TODO'ya ekleme yapılmadı.");
}

for (const entry of readdirSync(resolve(root, "src", "modules"), { withFileTypes: true })) {
  if (entry.isDirectory() && !slugs.includes(entry.name)) {
    console.warn(`  ! stale modül klasörü: src/modules/${entry.name} (artık metadata'da yok)`);
  }
}
console.log("▸ Bitti.");
