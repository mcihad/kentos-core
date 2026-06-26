import createClient, { type Client, type Middleware } from "openapi-fetch";

/**
 * Configuration every module client needs. The platform app (web/mobile) provides these
 * once: where the API lives, how to obtain the current access token, and what to do when
 * the server rejects auth. Keeping this here (not a global singleton) keeps clients
 * testable and lets web/mobile supply platform-specific token storage.
 */
export interface ApiClientOptions {
  /** API base URL, e.g. "http://localhost:5080" (no trailing slash needed). */
  baseUrl: string;
  /** Returns the current Bearer access token, or null/undefined when anonymous. */
  getToken?: () => string | null | undefined | Promise<string | null | undefined>;
  /** Called on a 401 (e.g. trigger a refresh or redirect to login). */
  onUnauthorized?: () => void;
  /** Called on a 429 with the Retry-After seconds (if the server sent one). */
  onRateLimited?: (retryAfterSeconds: number | null) => void;
}

/** Kentos RFC7807 error body (see GlobalExceptionHandler). */
export interface ProblemDetails {
  status?: number;
  title?: string;
  detail?: string;
  type?: string;
  errorCode?: string;
  traceId?: string;
  /** Field → messages, present for 400 validation errors. */
  errors?: Record<string, string[]>;
}

/** Standard paged list envelope returned by every list endpoint. */
export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

/**
 * Builds a fully-typed openapi-fetch client for a module's `paths`, wired with the shared
 * auth/error middleware. Runtime-agnostic: the same client works in React (web) and React
 * Native. Per-module `index.ts` files call this with their generated `paths` type.
 */
// `Paths` is the generated `paths` interface; openapi-fetch constrains it to `{}` (an
// interface has no implicit index signature, so Record<string, unknown> would reject it).
export function createApiClient<Paths extends {}>(
  options: ApiClientOptions,
): Client<Paths> {
  const client = createClient<Paths>({ baseUrl: options.baseUrl });

  const middleware: Middleware = {
    async onRequest({ request }) {
      const token = await options.getToken?.();
      if (token) {
        request.headers.set("Authorization", `Bearer ${token}`);
      }
      return request;
    },
    onResponse({ response }) {
      if (response.status === 401) {
        options.onUnauthorized?.();
      } else if (response.status === 429) {
        const header = response.headers.get("Retry-After");
        options.onRateLimited?.(header ? Number.parseInt(header, 10) : null);
      }
      return response;
    },
  };

  client.use(middleware);
  return client;
}
