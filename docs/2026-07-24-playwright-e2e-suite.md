# 2026-07-24 — Add a Playwright end-to-end test suite

## Context

Unit and integration tests cover the backend thoroughly, and the frontend unit suite covers
components/services in isolation with mocked HTTP — but nothing exercised the real UI in a
real browser against the real, fully-composed Docker stack. Added a third real-browser tier
to catch the class of bug the other two structurally can't: template/selector mismatches,
routing/guard behavior, and cross-component wiring (e.g. does accepting an invitation in one
browser actually make the board appear for that user elsewhere) that only shows up when the
whole system is running together.

## What changed

- New `e2e/` package (`@playwright/test`), independent of the `frontend/` Angular workspace —
  it's a consumer of the running app, not part of it.
- `e2e/playwright.config.ts`: `baseURL` defaults to `http://localhost:4200` (the Docker
  Compose frontend). Deliberately does not start anything itself — bringing up the stack is
  left to whoever runs the suite (a developer already following the README's quick start, or
  the CI job).
- Four spec files:
  - `auth.spec.ts` — unauthenticated redirect to `/login`, register → sign out → sign back in,
    wrong password shows an error without navigating away.
  - `boards.spec.ts` — creating a board shows it in the grid as "created by you"; opening a
    board shows its (empty) Kanban columns.
  - `collaboration.spec.ts` — the same flow the README's "Try the full workflow" section walks
    through with curl, but through the real UI in two separate browser contexts (auth lives in
    `localStorage`, so each identity needs its own context): owner invites by email, teammate
    accepts from the notification bell, owner assigns a task, teammate sees it and moves it
    through the state machine, and — the actual point of the test — a Member never even sees
    the create-task button or an assignment dropdown in the DOM, not just a 403 from the API.
  - `helpers.ts` — `registerUser`/`loginAs`, and a `uniqueEmail` helper so re-running the suite
    against a stack whose Postgres volume wasn't wiped never collides with a prior run.
- `docker-compose.e2e.yml`: a compose overlay that only relaxes
  `RateLimiting:Auth:PermitLimit` (see
  [`docs/2026-07-24-rate-limit-login-and-register.md`](docs/2026-07-24-rate-limit-login-and-register.md))
  — six tests registering their own users would otherwise trip the real 10/minute default.
  Run with `docker compose -f docker-compose.yml -f docker-compose.e2e.yml up --build -d`; the
  plain `docker-compose.yml` a recruiter runs from the README stays untouched.
- `.github/workflows/ci.yml`: new `e2e` job, gated on `backend`/`frontend` passing, that builds
  the overlay stack, polls `/health`, runs the suite, uploads the Playwright HTML report as an
  artifact on any outcome, and tears the stack down. The Docker publish job now also depends on
  `e2e` passing.

## Two real bugs the suite caught before this doc was written

1. The notification bell only fetches on load and then polls every 20 seconds — a test that
   registered a teammate and immediately had the owner invite them found no notification,
   because the bell's initial fetch had already run before the invitation existed. Not a bug in
   the app (a 20s poll is a reasonable tradeoff), but a real gap in the *test*, fixed by
   reloading before checking the bell.
2. More interestingly: the owner's board-detail page fetches its member list once on load and
   never refreshes it. If the owner opens a board, invites someone, and that person accepts
   *without the owner reloading*, the owner's assignment dropdown genuinely doesn't include the
   new member until they refresh the page. The test works around this the same way a real user
   would (reload), but this is a legitimate, previously-unnoticed UX gap — worth fixing as
   real-time member-list updates in a future pass, e.g. by refreshing on window focus or via a
   SignalR event, rather than requiring a manual reload.

## Verification

- `npx playwright test` (against `docker compose -f docker-compose.yml -f
  docker-compose.e2e.yml up --build -d`): **6/6 passing**, run several times back to back to
  confirm no flakiness from the two-browser-context timing (the invite-then-reload race noted
  above was the only source of flakiness found, and is now fixed with an explicit wait on the
  invite form clearing rather than a fixed sleep).
