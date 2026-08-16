# 2026-08-14 — Demo seed, external configuration, and per-user personalization

Three gaps, all of them about the first ten minutes someone spends with this project rather
than about what it can do:

1. **Nothing to look at.** `docker compose up` landed you on a login screen, and past it, an
   empty board. The headline feature — a background worker detecting workload anomalies —
   needs *load* to detect, so it was invisible until you'd manually created two accounts,
   invited one to the other's board, and generated overdue work.
2. **Nothing configurable.** Ports, database credentials and the JWT signing key were hardcoded
   in `docker-compose.yml` and `appsettings.json`. Changing the Postgres port (the usual
   collision on a developer machine) meant editing tracked files.
3. **Nothing personal.** The only per-user setting was the avatar colour. The theme was fixed
   dark; there was no way to change your display name or password after registering.

## What changed

### A seeded demo workspace

`Infrastructure/Persistence/DemoDataSeeder` fills an empty database with three users, two
populated boards, an unanswered invitation waiting in the demo user's notification bell, and
enough overdue and concurrent in-progress work on one assignee to trip two of the three alert
rules on the worker's first cycle.

Two constraints shaped it:

- **It goes through the domain, not around it.** Boards, tasks and users are built with the
  same factories and walked through the same state machine the API uses (`Todo → InProgress →
  Done`, never assigned to a task that's already closed), and passwords are hashed with the
  registered `IPasswordHasher`. A seeded account is an ordinary account — there is no
  special-cased login path for it.
- **It only runs on an empty database.** Seeding a database someone has been using would
  resurrect data they deleted and duplicate their boards, so the seeder checks for any existing
  user and skips out.

One detail worth recording: the domain refuses to create a task whose due date is already in
the past, which is exactly what an "overdue" demo task needs. Rather than bypass the rule, the
seeder uses *today at 00:00 UTC* — the earliest date the rule accepts, and already overdue by
the time the worker looks.

### One-click demo access, without an auth bypass

The login screen needs to know whether this instance has a demo account, and the button needs
credentials. A `demo-login` endpoint would have been an authentication bypass to maintain
forever; instead `GET /api/config` (anonymous, read-only) reports whether a demo account exists
and, if so, its credentials — and the button performs an ordinary `POST /api/auth/login` with
them. Handing out that password is deliberate: it's a throwaway account on a seeded instance,
printed in the README anyway, and the alternative was a second way into the system.

The endpoint checks the *database*, not just the config flag: seeding enabled on a database
that already had users means no demo account exists, and advertising one would produce a button
that fails.

### Everything configurable, nothing hardcoded

`.env.example` documents every value; `docker-compose.yml` reads them as `${VAR:-default}`, so
a `.env` overrides anything and its absence changes nothing. Added along the way:

- `docker-compose.prebuilt.yml` — runs the GHCR images CI already publishes, turning "try it"
  from a full .NET + Angular build into a pull.
- The frontend now waits for the API's container healthcheck (`condition: service_healthy`)
  instead of merely for the container to exist, so nginx never proxies to a backend that's
  still applying migrations.
- A startup check: with `ASPNETCORE_ENVIRONMENT=Production`, the API refuses to boot on the
  built-in demo JWT key (or anything under 32 characters). The demo works with zero
  configuration precisely because that key is public, which is exactly why a real deployment
  must not inherit it.
- `Makefile`, `LICENSE` (MIT).

### Personalization

- **Light theme.** Every colour already resolved from design tokens in `styles.scss`, so the
  light theme is a token override under `:root[data-theme='light']` — no component needed a
  light-specific rule. `ThemeService` stores the choice per browser (it's a property of the
  device you're looking at, not of who you are) and falls back to the OS preference on a first
  visit. Accent hues are darker in light mode: `#a855f7` on white fails contrast for text.
- **Profile page** (`/profile`): display name, avatar colour, password. Changing the password
  requires the current one even though the request is already authenticated, so an unattended
  session can't lock the real owner out; the new password goes through the same
  `PasswordRules` as registration.
- Board detail finally shows **which board you're on** — it was a hardcoded "Board" heading.

## A real bug this surfaced

`ChangePasswordCommand` was the project's first command returning nothing, and its integration
test caught a weak password sailing through with a `204`. The cause was in
`ValidationBehavior`:

```csharp
where TRequest : IRequest<TResponse>   // before
```

MediatR's parameterless `IRequest` does **not** derive from `IRequest<Unit>`, so this open
generic could not be closed for a void command — and DI silently skips a behavior it can't
close. Every future void command would have run with its validators never invoked, with no
error anywhere to say so. The constraint is now `where TRequest : notnull`, and
`ValidationBehaviorTests` pins it down twice: once by sending an invalid void command through a
real MediatR pipeline, and once by asserting that *every* request type in the Application
assembly can close the behavior.

## Verification

- Backend unit: **166 passing** (was 127 pre-change plus the in-flight branch's own additions).
- Backend integration: **49 passing**, including `DemoSeedTests` — which signs in through the
  advertised demo credentials against a real Postgres container and asserts the seeded board
  really does carry more overdue and more concurrent in-progress tasks than the rules' threshold.
- Frontend: **137 passing**, lint clean.
- Manually, against `docker compose up --build`: `/health` healthy, `/api/config` advertising
  the demo account, and after one worker cycle the launch board carried both a Warning ("User
  has 3 overdue tasks, exceeding the threshold of 2") and an Info ("3 tasks in progress at
  once") alert.
- README screenshots are generated from that running stack by `e2e/capture-screenshots.mjs`, so
  they can't drift from the real UI.

## Note on test parallelism

Each integration test class boots its own Postgres container, and this change added three more
classes. On a machine where Docker had just started, the default one-thread-per-core
parallelism produced container-start timeouts across *pre-existing* tests too. Capped at four
threads in `tests/IntegrationTests/xunit.runner.json`; the whole suite then passes with the
plain `dotnet test` command CI uses.
