# TaskFlow — Project Overview

Full breakdown of every library, module, and feature in the project, plus the exact
commands to build, run, and test each part. Companion to `README.md` (which covers the
"what and why") — this document is the "everything, in detail" reference.

---

## 1. Libraries

### 1.1 Backend — `src/Domain`
No external dependencies. Pure C#, by design (see ADR 0002) — the domain model must be
testable and understandable without knowing anything about EF Core, MediatR, or ASP.NET.

### 1.2 Backend — `src/Application`
| Library | Version | Purpose |
|---|---|---|
| `MediatR` | 12.4.1 | CQRS dispatch — each Command/Query has exactly one handler |
| `FluentValidation` | 11.11.0 | Declarative validation rules per command |
| `FluentValidation.DependencyInjectionExtensions` | 11.11.0 | Auto-registers all validators found in the assembly |
| `Microsoft.EntityFrameworkCore` | 9.0.1 | Provides `DbSet<T>` for the `ITaskFlowDbContext` abstraction |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 9.0.1 | `IServiceCollection` extension point (`AddApplication`) |

### 1.3 Backend — `src/Infrastructure`
| Library | Version | Purpose |
|---|---|---|
| `Microsoft.EntityFrameworkCore` | 9.0.1 | ORM |
| `Microsoft.EntityFrameworkCore.Design` | 9.0.1 | Enables `dotnet ef migrations` tooling |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 9.0.4 | PostgreSQL provider for EF Core |
| `Polly` | 8.5.0 | Resilience primitives (retry policies) available for outbound calls |
| *(FrameworkReference)* `Microsoft.AspNetCore.App` | shared framework | Lets this class library reference SignalR's `Hub` base class (`AlertsHub`) |

### 1.4 Backend — `src/Api`
| Library | Version | Purpose |
|---|---|---|
| `Swashbuckle.AspNetCore` | 6.9.0 | Swagger/OpenAPI UI at `/swagger` |
| `Serilog.AspNetCore` | 8.0.3 | Structured logging |
| `Serilog.Sinks.Console` | 6.0.0 | Console log sink (Docker-friendly) |
| `AspNetCore.HealthChecks.NpgSql` | 9.0.0 | `/health` endpoint checks real DB connectivity |
| `Microsoft.EntityFrameworkCore.Design` | 9.0.1 | Required at the startup project for `dotnet ef` |

### 1.5 Backend — `tests/UnitTests`
| Library | Version | Purpose |
|---|---|---|
| `xunit` | 2.9.2 | Test framework |
| `xunit.runner.visualstudio` | 2.8.2 | Test discovery/runner integration |
| `FluentAssertions` | 6.12.2 | Readable assertions (`.Should().Be(...)`) |
| `Microsoft.EntityFrameworkCore.InMemory` | 9.0.1 | In-memory fake `ITaskFlowDbContext` for Application handler tests |
| `Microsoft.NET.Test.Sdk` | 17.12.0 | Test SDK/host |

### 1.6 Backend — `tests/IntegrationTests`
| Library | Version | Purpose |
|---|---|---|
| `Microsoft.AspNetCore.Mvc.Testing` | 9.0.0 | `WebApplicationFactory<Program>` — boots the real API in-memory |
| `Testcontainers.PostgreSql` | 4.1.0 | Spins up a real, disposable Postgres container per test run |
| `xunit`, `FluentAssertions`, `Microsoft.NET.Test.Sdk` | (as above) | Test framework |

### 1.7 Frontend — `frontend/package.json`
| Library | Version | Purpose |
|---|---|---|
| `@angular/core`, `common`, `compiler`, `forms`, `platform-browser*`, `router` | ^19.2.0 | Angular framework |
| `@microsoft/signalr` | ^10.0.0 | Real-time client for `AlertsHub` |
| `rxjs` | ~7.8.0 | Reactive primitives (used under the hood by `HttpClient`) |
| `zone.js` | ~0.15.0 | Angular's change-detection zone |
| `karma`, `karma-chrome-launcher`, `karma-jasmine`, `jasmine-core` | (dev) | Unit test runner |
| `@angular/cli`, `@angular-devkit/build-angular` | ^19.2.27 (dev) | Build tooling |

---

## 2. Modules

### 2.1 `Domain` — business rules with zero infrastructure dependencies
- **Entities**: `User`, `ProjectBoard`, `BoardMember`, `BoardInvitation`, `Notification`,
  `TaskItem`, `AlertRule`, `Alert`, `LoadMetric`
- **`TaskItem`'s state machine**: `Todo -> InProgress -> {Blocked, Done, Cancelled}`, enforced
  by `IsValidTransition` — no handler or UI can force an invalid transition
- **`BoardInvitation`'s lifecycle**: `Pending -> {Accepted, Declined}` via `InvitationStatus`,
  enforced the same way — a resolved invitation can't be re-answered
- **`BoardRole`**: `Owner` / `Member`, held per `BoardMember` row (a user can be Owner of one
  board and Member of another)
- **`Result<T>`**: explicit success/failure return type for expected business-rule
  failures, avoiding exceptions for control flow
- **Domain events**: `TaskCompletedEvent`, raised when a task transitions to `Done` and
  dispatched through a real MediatR-based mechanism (see
  [`docs/2026-07-23-task-completed-event-dispatch.md`](docs/2026-07-23-task-completed-event-dispatch.md))

### 2.2 `Application` — CQRS use cases
One folder per aggregate, one subfolder per use case:
- **`Tasks`**: `CreateTask` (Owner-only), `TransitionTaskState`, `AssignTask` (Owner-only),
  `GetBoardTasks`
- **`Boards`**: `CreateBoard`, `GetBoards`, `InviteBoardMember`, `RespondToBoardInvitation`,
  `UpdateBoardMemberRole`, `RemoveBoardMember`, `GetBoardMembers`
- **`Notifications`**: `GetNotifications`, `MarkNotificationRead`
- **`Alerts`**: `GetBoardAlerts`, `MarkAlertRead`
- **`AlertRules`**: `CreateAlertRule`
- **`Users`**: `CreateUser`, `Login`
- **`Common`**: `ITaskFlowDbContext`, `IAlertNotifier`, `IDateTimeProvider`, `IBoardAuthorizer`
  (interfaces Infrastructure implements; `IBoardAuthorizer` is the single chokepoint every
  handler calls for `EnsureMemberAsync`/`EnsureOwnerAsync` role checks), `ValidationBehavior`
  (MediatR pipeline), custom exceptions (`NotFoundException`, `ValidationException`,
  `ForbiddenException` -> 403)

### 2.3 `Infrastructure` — the anomaly-detection engine + persistence
- **`Persistence`**: `TaskFlowDbContext` + one `IEntityTypeConfiguration<T>` per entity
  (Fluent API mappings, indexes, enum-as-string conversions)
- **`Workers/LoadMonitorWorker`**: `BackgroundService` — every `IntervalSeconds` (config),
  snapshots every board's load into `LoadMetric`, then evaluates enabled `AlertRule`s and
  raises + broadcasts `Alert`s, with deduplication so a standing condition doesn't
  re-alert every cycle
- **`Workers/AlertEvaluators`** (Strategy pattern, see ADR 0004):
  - `OverdueTasksThresholdEvaluator` — per-user overdue task count vs threshold
  - `BoardLoadSpikeEvaluator` — % growth in active tasks vs a past snapshot
  - `ConcurrentInProgressThresholdEvaluator` — per-user concurrent in-progress count
- **`Realtime`**: `AlertsHub` (SignalR hub, group-per-board) + `SignalRAlertNotifier`
  (implements `IAlertNotifier`)
- **`Services/DateTimeProvider`**: the only place `DateTime.UtcNow` is called in
  production code, so tests can substitute a fake clock
- **`Services/PasswordHasher`**: PBKDF2-HMAC-SHA256 password hashing (custom, not ASP.NET
  Identity's `PasswordHasher<TUser>`, since `User` has a private constructor)
- **`Services/JwtTokenGenerator`**: issues bearer tokens via `System.IdentityModel.Tokens.Jwt`
- **`Services/CurrentUserService`**: reads the authenticated user's id/claims from
  `IHttpContextAccessor` — the real, request-scoped `ICurrentUserService` implementation
- **`JwtOptions`**: signing key/issuer/audience/expiry, bound from configuration

### 2.4 `Api` — HTTP surface
- **Controllers**: `AuthController`, `UsersController`, `BoardsController`,
  `InvitationsController`, `NotificationsController`, `TasksController`, `AlertsController`
- **`Middleware/SecurityHeadersMiddleware`**: adds defensive response headers (strict CSP,
  HSTS, X-Frame-Options, X-Content-Type-Options, Referrer-Policy, Permissions-Policy) to every
  response, registered first so it also covers error responses
- **`Middleware/ExceptionHandlingMiddleware`**: converts `NotFoundException` -> 404,
  `ValidationException` -> 400, `ForbiddenException` -> 403, anything else -> 500, all as
  RFC 7807 `ProblemDetails`
- **`Program.cs`**: composition root — registers Serilog, Swagger, JWT bearer auth, health
  checks, CORS (for local `ng serve`), a `JsonStringEnumConverter` for enum-as-string
  request/response bodies, applies EF Core migrations on startup, maps controllers +
  `AlertsHub` + `/health`

### 2.5 `frontend` — Angular 19 SPA
- **`core/models`**: TypeScript mirrors of every backend DTO/enum
  (`TaskDto`, `BoardDto`, `AlertDto`, `NotificationDto`, `TaskState`, `TaskPriority`,
  `AlertSeverity`, `BoardRole`)
- **`core/services`**:
  - `CurrentUserService` — real JWT-backed identity (register/login/token), persisted to
    `localStorage`
  - `BoardService`, `TaskService` — thin HTTP wrappers; `BoardService` also holds a shared
    `boards` signal so any component (e.g. the notification bell after accepting an invite)
    can trigger a refresh that every consumer sees
  - `NotificationService` — REST fetch, exposes a shared `notifications` signal
  - `AlertService` — REST fetch + SignalR connection lifecycle (`connectToBoard`/`disconnect`), exposes a live `alerts` signal
- **`core/interceptors`** / **`core/guards`**: an `HttpInterceptorFn` attaches the JWT to
  every request and signs the user out on 401; a `CanActivateFn` guard protects authenticated
  routes
- **`features/auth`**: login/register forms
- **`features/boards/board-list`**: board grid (Owner-only create-board form), each card
  showing "created by you" vs "created by {owner}"
- **`features/boards/board-detail`**: Kanban columns (Todo/In progress/Blocked/Done,
  Cancelled hidden behind a counter), Owner-only task creation form, per-task valid-transition
  buttons (mirrors the backend state machine exactly, available to whoever the task is
  assigned to), live alert console with a connection-status indicator
- **`shared/notification-bell`**: mounted in the app shell on every page — lists
  invitations/assignments/state-changes, lets the user accept/decline an invitation inline

### 2.6 `tests`
- **`UnitTests/Domain`**: `TaskItemTests` (full state-machine truth table via
  `[Theory]`/`[InlineData]`), `UserTests`, `AlertRuleTests`, `ResultTests`,
  `BoardInvitationTests`, `ColorPaletteTests`
- **`UnitTests/Application`**: handler tests for every command/query above (including
  Owner/Member authorization outcomes, e.g. `CreateTaskCommandHandlerTests` Forbidden case)
  against an EF Core InMemory-backed fake context — **104 tests**
- **`IntegrationTests`**: full HTTP round-trips against a real, disposable Postgres
  container — auth (register/login), the login/register rate limiter, security headers, board
  membership/roles, invitations, notifications, task creation/assignment authorization,
  SignalR hub authentication/board-membership checks — **24 tests**
- **`e2e`** (Playwright, `@playwright/test`): drives real Chromium browsers against the actual
  Docker stack (not a mocked backend) — auth redirects, register/sign-out/sign-in, wrong
  password, board creation, and a two-browser-context walk through the full owner/member
  workflow (invite by email, accept from the notification bell, assign, move through the
  state machine, and the role boundary itself) — **6 tests**. Run via
  `docker compose -f docker-compose.yml -f docker-compose.e2e.yml up --build -d` then
  `cd e2e && npm ci && npx playwright test`

### 2.7 Infrastructure-as-config
- **`docker-compose.yml`**: orchestrates `postgres`, `api`, `frontend` — each with a
  `restart: unless-stopped` policy and a `deploy.resources.limits` (cpus/memory) cap
- **`docker-compose.e2e.yml`**: overlay used only by the Playwright suite — relaxes the
  login/register rate limit so E2E tests registering their own users don't trip it, without
  touching the realistic default in `docker-compose.yml`
- **`Dockerfile.api`** / **`Dockerfile.frontend`**: multi-stage builds (SDK/Node -> slim runtime)
- **`docker/nginx.conf`**: serves the Angular build, proxies `/api/` and `/hubs/` to the API container (avoids CORS in the Docker demo)
- **`.github/workflows/ci.yml`**: backend build+test (with a real Postgres service
  container), frontend build+test, end-to-end tests (full Docker stack + Playwright), Docker
  build & push to GHCR on `main` + Trivy vulnerability scan of both published images

---

## 3. Key features (functional summary)

1. **Real authentication & authorization**: JWT bearer login/registration (rate-limited per
   IP against brute-force attempts); board-scoped Owner/Member roles enforced through a single
   `IBoardAuthorizer` chokepoint — only Owners create tasks, assign them, manage membership,
   and configure alert rules; the SignalR hub itself requires the same JWT and re-checks board
   membership before letting a connection join a board's alert group
2. **Invitations & notifications**: Owners invite teammates by email (works even if they
   haven't registered yet); invitees get a real in-app notification and must accept before
   joining; a notification center also surfaces task assignments and state changes, never
   for your own actions
3. **Task management**: create, assign, and move tasks through an explicit state machine on a board
4. **Workload anomaly detection**: a background worker continuously watches every board and raises alerts for:
   - a user with too many overdue tasks
   - a board whose active-task count spiked abnormally within a time window
   - a user juggling too many concurrent in-progress tasks (context-switch risk)
5. **Real-time delivery**: alerts appear in the UI instantly via SignalR, no polling
6. **Configurable thresholds**: alert rules (threshold + evaluation window) are created per board via the API, not hardcoded
7. **Self-contained demo**: `docker compose up --build` is the entire setup — migrations apply automatically on API startup

---

## 4. Commands reference

### 4.1 Backend — build & run
```bash
dotnet restore
dotnet build
dotnet run --project src/Api          # runs API standalone on http://localhost:5080 (needs a reachable Postgres)
```

### 4.2 Backend — migrations
```bash
dotnet tool install --global dotnet-ef        # one-time
dotnet ef migrations add <Name> --project src/Infrastructure --startup-project src/Api
dotnet ef database update --project src/Infrastructure --startup-project src/Api   # manual apply (not needed with docker compose - Program.cs auto-migrates)
```

### 4.3 Backend — tests
```bash
dotnet test tests/UnitTests/TaskFlow.UnitTests.csproj
dotnet test tests/IntegrationTests/TaskFlow.IntegrationTests.csproj   # requires Docker running (Testcontainers)
dotnet test                                                           # runs every test project in the solution
```

### 4.4 Frontend — build & run
```bash
cd frontend
npm ci
npm start                 # ng serve, http://localhost:4200, points at http://localhost:5080 (environment.development.ts)
npm run build             # production build to dist/frontend/browser
```

### 4.5 Frontend — tests
```bash
cd frontend
npx ng test                                    # interactive (Karma + Chrome)
npx ng test --watch=false --browsers=ChromeHeadless   # CI mode
```

### 4.6 Full stack via Docker
```bash
docker compose up --build      # first run / after any Dockerfile or dependency change
docker compose up              # subsequent runs
docker compose down            # stop and remove containers (keeps the Postgres volume)
docker compose down -v         # stop and wipe all data too
docker compose logs api -f     # tail API logs (Serilog output)
```

---

## 5. Future work (explicitly out of scope for v1)

- Renaming or deleting a board (the domain model supports renaming internally, but no
  command/endpoint exposes it yet; deletion isn't modeled at all)
- Promoting/demoting a member beyond `UpdateBoardMemberRole`'s Owner/Member toggle (e.g. a
  finer-grained permission model, or transferring board ownership)
- A hosted, one-click live demo (today the project is Docker-first: `docker compose up
  --build` is the fastest path to trying it)

## 6. Feature history

Dated write-ups of individual features and fixes, in the order they were built, each with
what changed, why, and how it was verified:

- [`docs/2026-07-23-frontend-tests-and-ci-fixes.md`](docs/2026-07-23-frontend-tests-and-ci-fixes.md) — the frontend unit test suite (previously missing) and two real CI bugs it uncovered
- [`docs/2026-07-23-task-completed-event-dispatch.md`](docs/2026-07-23-task-completed-event-dispatch.md) — `TaskCompletedEvent` dispatched to a real handler
- [`docs/2026-07-23-assign-task-to-any-user-in-ui.md`](docs/2026-07-23-assign-task-to-any-user-in-ui.md) — assigning tasks to any user from the UI
- [`docs/2026-07-23-enum-json-serialization-fix.md`](docs/2026-07-23-enum-json-serialization-fix.md) — the string-vs-numeric enum bug behind a live 400 on task creation
- [`docs/2026-07-23-real-authentication.md`](docs/2026-07-23-real-authentication.md) — JWT bearer auth, password hashing, login/register
- [`docs/2026-07-23-board-membership-and-roles.md`](docs/2026-07-23-board-membership-and-roles.md) — `BoardMember`, `BoardRole`, `IBoardAuthorizer`
- [`docs/2026-07-23-board-and-user-colors.md`](docs/2026-07-23-board-and-user-colors.md) — the color palette and per-board/user accent colors
- [`docs/2026-07-23-invitations-notifications-and-owner-only-assignment.md`](docs/2026-07-23-invitations-notifications-and-owner-only-assignment.md) — email invitations, the notification center, Owner-only task assignment
- [`docs/2026-07-23-owner-only-task-creation-and-board-ownership-display.md`](docs/2026-07-23-owner-only-task-creation-and-board-ownership-display.md) — Owner-only task creation, the board-list live-refresh bug fix, and "created by" display
- [`docs/2026-07-24-secure-signalr-hub-with-jwt-and-board-membership.md`](docs/2026-07-24-secure-signalr-hub-with-jwt-and-board-membership.md) — JWT-authenticated the SignalR hub and enforced board membership on `JoinBoard`
- [`docs/2026-07-24-rate-limit-login-and-register.md`](docs/2026-07-24-rate-limit-login-and-register.md) — per-IP rate limiting on the two anonymous endpoints
- [`docs/2026-07-24-playwright-e2e-suite.md`](docs/2026-07-24-playwright-e2e-suite.md) — a real-browser end-to-end test tier against the full Docker stack, and a real UX gap it surfaced (the owner's member list doesn't refresh live)
- [`docs/2026-07-24-live-member-list-and-invite-feedback.md`](docs/2026-07-24-live-member-list-and-invite-feedback.md) — fixed that gap (the member list now polls, like notifications already do) and added an explicit "Invitation sent" confirmation
- [`docs/2026-07-24-docker-hardening.md`](docs/2026-07-24-docker-hardening.md) — non-root frontend container, restart policies, resource limits, and Trivy image scanning in CI
- [`docs/2026-07-24-security-headers.md`](docs/2026-07-24-security-headers.md) — defensive HTTP headers on API + frontend, and a latent test-isolation bug it surfaced (integration tests were hitting the wrong Postgres)
