# Deploying a public demo

Everything here is platform-neutral: TaskFlow ships as two images plus a Postgres database, and
any host that can run a container and hand it environment variables will do. No provider-specific
manifest is committed, on purpose — the moment one is, it rots.

## What you're deploying

| Piece | Image | Listens on |
|---|---|---|
| API | `Dockerfile.api` → `ghcr.io/<owner>/taskflow/api` | `8080` |
| Frontend (nginx) | `Dockerfile.frontend` → `ghcr.io/<owner>/taskflow/frontend` | `8080` |
| Database | any PostgreSQL 16 | `5432` |

Both images are published by CI on every push to `main`, so a host that deploys from a registry
needs no build step at all.

## Topology: the frontend proxies the API

Only the **frontend** needs a public URL. nginx serves the Angular build and proxies `/api` and
`/hubs` to the API over the platform's internal network, which is how the Docker demo already
works — one origin, no CORS, and the JWT never crosses a third-party domain.

```
  visitor ──▶ frontend (public)  ──internal──▶  api  ──▶  postgres
                 nginx: / → static files
                        /api, /hubs → ${API_UPSTREAM}
```

Point `API_UPSTREAM` at whatever internal address your platform gives the API container
(`taskflow-api.internal:8080`, `api.railway.internal:8080`, a service DNS name — the shape
varies, the idea doesn't). The value is substituted into the nginx config at container start,
so it's configuration, not a rebuild.

> **The alternative — exposing the API publicly and calling it directly from the browser —
> needs a frontend rebuild**, because the Angular production build has `apiUrl: '/api'` compiled
> in (`frontend/src/environments/environment.ts`). It also brings back CORS and a second public
> surface. Prefer the proxy.

## Environment variables

On the **API**:

| Variable | Value for a public demo | Why |
|---|---|---|
| `ConnectionStrings__Postgres` | your database's connection string | Migrations apply automatically at start-up |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Turns Swagger off and enforces the JWT check below |
| `Jwt__Secret` | **a real secret, 32+ chars** | The API refuses to start in Production with the built-in demo key. Generate one: `openssl rand -base64 48` |
| `Seed__Enabled` | `true` | Otherwise your public demo is an empty login screen |
| `Seed__Password` | your choice | Printed in the README for visitors — treat it as public |
| `Seed__ResetIntervalHours` | `168` | Wipes and rebuilds the workspace weekly, so the link keeps showing the intended state |
| `LoadMonitor__IntervalSeconds` | `20`–`60` | Lower makes alerts appear sooner for a visitor |

On the **frontend**:

| Variable | Value |
|---|---|
| `API_UPSTREAM` | the API's internal `host:port` |

## The weekly reset

`Seed__ResetIntervalHours` is what makes a public demo maintainable: `DemoResetWorker` checks
every 15 minutes whether the seeded workspace has aged past the interval, and if it has, deletes
**everything** — including accounts and boards visitors created — and re-seeds from scratch.

Two details worth knowing:

- The age is measured from the demo owner's `CreatedAtUtc`, not from a timer started at boot. A
  free-tier host that sleeps between visits would never survive long enough to reach a weekly
  tick; a persisted timestamp is re-evaluated every time the process wakes.
- It refuses to run if there's no demo owner in the database, so pointing this build at a real
  database with the flag left on doesn't wipe it. That's a backstop, not a licence — leave
  `Seed__ResetIntervalHours` at `0` anywhere that holds data you care about.

## Free-tier realities

- **Hosts that sleep on idle** (most free tiers) add a cold start of roughly 30–60 seconds to
  the first visit, and the API has to apply migrations and possibly re-seed on wake. If the demo
  URL is going on a CV, this is the thing that will make someone think the app is broken.
  Mitigation: a keep-alive ping to `/health`, or a paid always-on instance.
- **Free managed Postgres often expires** (30–90 days is typical). When it does, the next start
  begins with an empty database — which the seeder simply refills. Nothing to restore, but the
  connection string changes.
- The API image runs migrations at start-up, so a fresh database needs no manual step.

## Checklist

1. Create the Postgres database; copy its connection string.
2. Deploy the API image with the variables above. Confirm `GET /health` returns `Healthy`.
3. Deploy the frontend image with `API_UPSTREAM` set to the API's internal address.
4. Open the public URL and confirm the login screen offers **Explore the demo workspace** — if
   it doesn't, `GET /api/config` will tell you whether the API thinks a demo account exists.
5. Put the URL in `README.md`, at the top.
