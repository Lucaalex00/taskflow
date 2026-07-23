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
- **Entities**: `User`, `ProjectBoard`, `TaskItem`, `AlertRule`, `Alert`, `LoadMetric`
- **`TaskItem`'s state machine**: `Todo -> InProgress -> {Blocked, Done, Cancelled}`, enforced
  by `IsValidTransition` — no handler or UI can force an invalid transition
- **`Result<T>`**: explicit success/failure return type for expected business-rule
  failures, avoiding exceptions for control flow
- **Domain events**: `TaskCompletedEvent`, raised (not yet dispatched — see "Future work")
  when a task transitions to `Done`

### 2.2 `Application` — CQRS use cases
One folder per aggregate, one subfolder per use case:
- **`Tasks`**: `CreateTask`, `TransitionTaskState`, `AssignTask`, `GetBoardTasks`
- **`Boards`**: `CreateBoard`, `GetBoards`
- **`Alerts`**: `GetBoardAlerts`, `MarkAlertRead`
- **`AlertRules`**: `CreateAlertRule`
- **`Users`**: `CreateUser`
- **`Common`**: `ITaskFlowDbContext`, `IAlertNotifier`, `IDateTimeProvider` (interfaces
  Infrastructure implements), `ValidationBehavior` (MediatR pipeline), custom exceptions
  (`NotFoundException`, `ValidationException`)

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

### 2.4 `Api` — HTTP surface
- **Controllers**: `UsersController`, `BoardsController`, `TasksController`, `AlertsController`
- **`Middleware/ExceptionHandlingMiddleware`**: converts `NotFoundException` -> 404,
  `ValidationException` -> 400, anything else -> 500, all as RFC 7807 `ProblemDetails`
- **`Program.cs`**: composition root — registers Serilog, Swagger, health checks, CORS
  (for local `ng serve`), applies EF Core migrations on startup, maps controllers +
  `AlertsHub` + `/health`

### 2.5 `frontend` — Angular 19 SPA
- **`core/models`**: TypeScript mirrors of every backend DTO/enum
  (`TaskDto`, `BoardDto`, `AlertDto`, `TaskState`, `TaskPriority`, `AlertSeverity`)
- **`core/services`**:
  - `CurrentUserService` — demo-scope identity (no real auth in the brief), persisted to `localStorage`
  - `BoardService`, `TaskService` — thin HTTP wrappers
  - `AlertService` — REST fetch + SignalR connection lifecycle (`connectToBoard`/`disconnect`), exposes a live `alerts` signal
- **`features/boards/board-list`**: onboarding form + board grid + create-board form
- **`features/boards/board-detail`**: Kanban columns (Todo/In progress/Blocked/Done,
  Cancelled hidden behind a counter), task creation form, per-task valid-transition
  buttons (mirrors the backend state machine exactly), live alert console with a
  connection-status indicator

### 2.6 `tests`
- **`UnitTests/Domain`**: `TaskItemTests` (full state-machine truth table via
  `[Theory]`/`[InlineData]`), `UserTests`, `AlertRuleTests`, `ResultTests`
- **`UnitTests/Application`**: `CreateTaskCommandHandlerTests` against an EF Core
  InMemory-backed fake context
- **`IntegrationTests`**: `TasksEndpointsTests` — full HTTP round-trip
  (create user -> create board -> create task -> list -> transition state) against a real,
  disposable Postgres container

### 2.7 Infrastructure-as-config
- **`docker-compose.yml`**: orchestrates `postgres`, `api`, `frontend`
- **`Dockerfile.api`** / **`Dockerfile.frontend`**: multi-stage builds (SDK/Node -> slim runtime)
- **`docker/nginx.conf`**: serves the Angular build, proxies `/api/` and `/hubs/` to the API container (avoids CORS in the Docker demo)
- **`.github/workflows/ci.yml`**: backend build+test (with a real Postgres service
  container), frontend build+test, Docker build & push to GHCR on `main`

---

## 3. Key features (functional summary)

1. **Task management**: create, assign, and move tasks through an explicit state machine on a board
2. **Workload anomaly detection**: a background worker continuously watches every board and raises alerts for:
   - a user with too many overdue tasks
   - a board whose active-task count spiked abnormally within a time window
   - a user juggling too many concurrent in-progress tasks (context-switch risk)
3. **Real-time delivery**: alerts appear in the UI instantly via SignalR, no polling
4. **Configurable thresholds**: alert rules (threshold + evaluation window) are created per board via the API, not hardcoded
5. **Self-contained demo**: `docker compose up --build` is the entire setup — migrations apply automatically on API startup

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

- Real authentication (`CurrentUserService` is demo-scope, no password/JWT)
- Dispatching `TaskCompletedEvent` to actual subscribers (the event is raised but not yet consumed by anything - infrastructure for this exists but no handler is registered)
- Frontend unit tests for components/services (only the CLI-generated `app.component.spec.ts` exists today)
- Assigning tasks to a specific user other than "self" from the UI (the API supports it; the UI only exposes "assign to me")
