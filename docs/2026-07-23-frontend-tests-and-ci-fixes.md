# 2026-07-23 — Frontend unit tests + CI fixes

## What changed

### 1. Frontend unit test suite (previously only the CLI-generated `app.component.spec.ts` existed)

Added specs for every `core/services` and `features/boards` file:

| File | Covers |
|---|---|
| `core/services/board.service.spec.ts` | `getAll`, `create` via `HttpTestingController` |
| `core/services/task.service.spec.ts` | `getBoardTasks`, `create`, `transitionState`, `assign` |
| `core/services/current-user.service.spec.ts` | localStorage-backed identity: initial state, restore, `register`, `signOut` |
| `core/services/alert.service.spec.ts` | REST (`getBoardAlerts`, `markRead`) + SignalR lifecycle (`connectToBoard`, board switching, incoming `AlertRaised` events, `disconnect`) |
| `features/boards/board-list/board-list.component.spec.ts` | registration flow, board loading/creation, navigation, error paths |
| `features/boards/board-detail/board-detail.component.spec.ts` | task loading/grouping by column, state transitions, assignment, alert read, teardown |

**Approach**:
- Service tests use `provideHttpClient()` + `provideHttpClientTesting()` (real HTTP layer, fake backend).
- Component tests mock all injected services with `jasmine.createSpyObj` — isolates component behavior from HTTP/SignalR details already covered by the service tests.
- `AlertService`'s SignalR connection isn't injectable, so its test spies directly on `signalR.HubConnectionBuilder.prototype.withUrl` to substitute a fake `HubConnection`, and uses `fakeAsync`/`tick()` to deterministically flush the chained awaits (`start()` → `invoke()` → the alerts HTTP call) instead of racing real microtask timing.

Also fixed `app.component.spec.ts`, which still asserted on `app.title` / `'Hello, frontend'` — leftover from the Angular CLI scaffold, never updated after `AppComponent` became the real app shell. This is the root cause of one of the two CI failures below.

### 2. Two CI bugs found while verifying the above

Both `.github/workflows/ci.yml` jobs were red on the first push. Neither was a flaky-test issue — both were real bugs:

- **Frontend**: `app.component.spec.ts` asserted on properties/text that no longer exist on `AppComponent` (see above) → fixed by rewriting the spec against the real template (`.brand__name`, `<router-outlet>`).
- **Backend**: `ExceptionHandlingMiddleware.WriteProblemAsync` set `context.Response.ContentType = "application/problem+json"` and then called `Response.WriteAsJsonAsync(problemDetails)` — but `WriteAsJsonAsync`'s default overload unconditionally overwrites `ContentType` with `application/json` unless you pass a `contentType` explicitly. Every error response was therefore served as `application/json` instead of the RFC 7807 `application/problem+json` it advertised, which is what `TasksEndpointsTests.CreateTask_OnNonExistentBoard_Returns404WithProblemDetails` caught. Fixed by passing `contentType: "application/problem+json"` to `WriteAsJsonAsync`.

## Verification

- `dotnet test` (unit: 28 passed, integration: 3 passed, requires Docker for Testcontainers)
- `npx ng test --watch=false --browsers=ChromeHeadless` (39 passed)
- `npx ng build --configuration production` (succeeds)
