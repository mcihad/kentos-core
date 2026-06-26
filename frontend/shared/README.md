# @kentos/shared

Backend modüllerinden **otomatik üretilen** tipli API client'ları. Web ve mobile bunu
ortak tüketir. Elle model/servis yazılmaz — kaynak doğruluk backend'in OpenAPI'sidir.

## Üretim

API ayaktayken (`make run`), repo kökünden:

```bash
make gen-frontend        # = cd frontend/shared && pnpm gen && pnpm typecheck
```

veya doğrudan:

```bash
pnpm install
KENTOS_API_URL=http://localhost:5080 pnpm gen
pnpm typecheck
```

`gen` ne yapar (bkz. `scripts/gen.mjs`):
1. `/api/v1/metadata`'dan **lisanslı modülleri keşfeder** (backend ile aynı kaynak).
2. Yeni OpenAPI'yi önceki `openapi/<slug>.json` ile **diff'ler**.
3. `openapi/<slug>.json`'u günceller (tekrar üretilebilirlik için commit'lenir).
4. `openapi-typescript` → `src/modules/<slug>/schema.ts` (tipler/modeller).
5. `src/modules/<slug>/sdk.ts` (adlandırılmış metotlar; **her metodun üzerinde `@permission`** JSDoc).
6. `src/modules/<slug>/queries.ts` (**TanStack Query hook'ları**; GET→`useQuery`, yazma→`useMutation` + otomatik invalidation; yine `@permission`).
7. `src/modules/<slug>/index.ts` (client + SDK + Query fabrikaları).
8. `src/modules/index.ts` barrel'ını günceller.
9. Değişiklikleri (yeni endpoint/metot, yeni varlık alanları, kaldırılanlar) **`frontend/TODO.md`**'ye ekler.

> **Kural:** `schema.ts`, `sdk.ts`, `index.ts` ve `openapi/*.json` **üretilmiştir, elle
> düzenlenmez** (her `gen`'de yeniden yazılır). Özel kod ayrı dosyalara yazılır. CI'da
> `pnpm gen` çalıştırıp diff varsa fail edin → tipler her zaman canlı API ile birebir kalır.

## TODO akışı (iki taraf senkron)

Backend'de bir resource eklenince/güncellenince (`/create-resource`, `/update-resource`
skill'leri son adımda `make gen-frontend` çağırır) değişiklikler **`frontend/TODO.md`**'ye
yazılır: hangi varlığa hangi alan eklendi, hangi endpoint/metot eklendi ve **hangi
permission ile erişilir**. Frontend tarafında bu maddeler yapılır, işaretlenir ve bölüm
silinir; dosya boşalınca temizlenir. Böylece bir tarafı yapınca diğer tarafta sessiz açık
kalmaz.

## Yapı

```
src/
  client.ts            createApiClient (openapi-fetch + auth/401/429 middleware),
                       ProblemDetails, PagedResponse
  index.ts             paylaşılan barrel (client)
  modules/
    index.ts           AUTO: her modül bir namespace (export * as <slug>)
    hesap/
      schema.ts        AUTO: tipler (openapi-typescript)
      sdk.ts           AUTO: adlandırılmış metotlar + @permission JSDoc
      queries.ts       AUTO: TanStack Query hook'ları + @permission
      index.ts         AUTO: createHesapClient / createHesapApi / createHesapQueries
    settlement/ ...
openapi/               AUTO: indirilen OpenAPI dökümanları (commit'lenir)
```

## Kullanım

Hem web (React) hem mobile (React Native) `@tanstack/react-query` + `react` sağlar
(peer dependency). Uygulama bir kez `QueryClientProvider` ile sarmalanır.

### Önerilen: TanStack Query hook'ları

```ts
// 1) bir kez kur (token saklama platforma göre değişir):
import { createHesapApi, createHesapQueries } from "@kentos/shared/modules/hesap";

const sdk = createHesapApi({
  baseUrl: "http://localhost:5080",
  getToken: () => store.accessToken,        // web: localStorage, RN: SecureStore
  onUnauthorized: () => auth.refreshOrLogout(),
});
export const hesap = createHesapQueries(sdk);

// 2) bileşende kullan — cache, yenileme, mutation otomatik:
function Users() {
  const { data, isLoading } = hesap.useGetUsers({ params: { query: { page: 1, pageSize: 20 } } });
  const create = hesap.usePostUsers();   // başarıda users query'leri otomatik invalidate
  // create.mutate({ body: { ... } });
}
```

> Not: hook'lar nesne metodu olarak çağrılır (`hesap.useGetUsers()`). React çalışma zamanı
> için sorun değil; `eslint-plugin-react-hooks` kullanıyorsan member-hook'lara izin ver.

### Alternatif: ham SDK / client (React Query'siz)

```ts
const sdk = createHesapApi({ baseUrl, getToken });
const { data, error } = await sdk.getUsers({ params: { query: { page: 1, pageSize: 20 } } });
```
