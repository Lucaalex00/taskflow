# 2026-07-24 — Security HTTP headers (API + frontend), and a real test-isolation bug found along the way

## Context

Neither the API nor the nginx-served frontend set any defensive HTTP headers (CSP, HSTS,
X-Frame-Options, etc.). These are cheap, standard, and among the first things a security-minded
reviewer checks — their absence reads as "didn't think about the browser threat model."

## What changed

**API** (`SecurityHeadersMiddleware`, registered first in the pipeline so it applies even to
error responses):
- `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`,
  `Referrer-Policy: strict-origin-when-cross-origin`,
  `Permissions-Policy: geolocation=(), microphone=(), camera=()`,
  `Strict-Transport-Security: max-age=31536000; includeSubDomains`.
- A maximally strict CSP — `default-src 'none'; frame-ancestors 'none'` — since the API only
  ever returns JSON, never HTML a browser would render.

**Frontend** (`docker/nginx.conf`, `add_header ... always`):
- The same defensive headers, plus a CSP scoped to what the Angular app actually loads:
  same-origin scripts/XHR/**WebSocket** (`connect-src 'self'` covers the SignalR hub, since
  nginx proxies `/hubs/` on the same origin), Google Fonts (`style-src`/`font-src`), inline
  styles (Angular component styles), and `data:` images. `frame-ancestors 'none'`,
  `base-uri 'self'`, `form-action 'self'`.

**Angular build** (`angular.json`): disabled `optimization.styles.inlineCritical`. Angular's
critical-CSS inlining defers the non-critical stylesheet with a
`<link media="print" onload="this.media='all'">` trick — an **inline event handler**, which
the strict `script-src 'self'` CSP correctly blocks. Rather than weaken the CSP with
`'unsafe-hashes'`, dropping inlineCritical removes the inline handler entirely (negligible perf
cost for an app this size).

## Two bugs found while verifying

1. **Test-isolation bug (pre-existing, latent)**: adding `SecurityHeadersMiddleware` made the
   integration suite briefly fail to connect to Postgres — which surfaced that
   `AddInfrastructure` captured the connection string from its `IConfiguration` parameter *at
   registration time*, before `WebApplicationFactory` layers in its test-only
   `ConnectionStrings:Postgres` override (the Testcontainers instance). The tests had been
   silently passing only because a `docker compose` Postgres happened to be listening on
   `localhost:5432` — i.e. they were hitting the *wrong* database and never truly isolated.
   Fixed by resolving the connection string lazily from DI inside the `AddDbContext` factory
   (`sp.GetRequiredService<IConfiguration>()`), the same pattern already used for the auth rate
   limiter. Confirmed by running the suite with **no** compose Postgres up: now green (was red).
2. **CSP vs. inline handler** (above) — caught by an ad-hoc Playwright check that listened for
   console CSP-violation messages while loading a board; it also confirmed the SignalR
   connection dot goes live under the strict CSP, proving WebSocket isn't blocked.

## Verification

- `dotnet test`: 104 unit + **24 integration** (new `SecurityHeadersTests`: asserts each header
  and the strict CSP are present on the anonymous `/health` response).
- Live: `curl -D-` against both `http://localhost:5080/health` (API) and
  `http://localhost:4200/` (nginx) shows the full header set.
- E2E: all 6 Playwright tests still green against the hardened stack; a throwaway CSP-violation
  listener confirmed zero violations and a live SignalR connection.
