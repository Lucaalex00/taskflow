# 2026-07-23 — Dispatch TaskCompletedEvent to a real handler

## Context

`TaskItem.TransitionTo` already raised `TaskCompletedEvent` when a task moved to `Done`
(via `Entity.Raise`), but nothing ever published it — `IDomainEvent`'s own doc comment said
"Dispatched by infrastructure (e.g. after SaveChanges)", yet no such dispatch existed anywhere
in the codebase (confirmed by grep: only `Raise()` and the `Ignore(x => x.DomainEvents)` EF
mappings referenced it).

## What changed

Domain cannot depend on MediatR (ADR 0002 — zero external dependencies), so events are
raised as plain `IDomainEvent`s and only wrapped as MediatR notifications at the
Infrastructure boundary:

- **`Application/Common/Events/DomainEventNotification.cs`** — generic `INotification`
  wrapper around any `IDomainEvent`. Lives in Application (which already depends on MediatR),
  not Domain.
- **`Infrastructure/Persistence/TaskFlowDbContext.cs`** — overrides `SaveChangesAsync` to:
  collect entities with pending domain events, call `base.SaveChangesAsync`, then for each
  event wrap it via reflection (`DomainEventNotification<TConcreteEvent>`, since the static
  type is `IDomainEvent`) and publish through the injected `IPublisher`, then clear the
  entity's events so they aren't re-published on the next save.
- **`Application/Tasks/EventHandlers/TaskCompletedEventHandler.cs`** — the first real
  subscriber: logs task completion (`TaskId`, `BoardId`, `AssigneeId`) as a structured audit
  entry via `ILogger`. MediatR auto-discovers it (already scans the Application assembly in
  `AddApplication`), no registration needed.

Chose logging over reusing the `Alert`/`IAlertNotifier` pipeline: `Alert.Create` requires a
non-nullable `AlertRuleId` — alerts are architecturally tied 1:1 to an `AlertRule` match
raised by `LoadMonitorWorker`, not a general-purpose notification channel. Forcing a
"task completed" event through that FK would mean a schema change or a sentinel rule just to
fit an unrelated concept. A logged, decoupled handler demonstrates the dispatch mechanism
without stretching an existing concept to a shape it wasn't built for. More subscribers
(e.g. a real notification) can be added later without touching `TaskItem` or this handler.

## Verification

- `tests/UnitTests/Infrastructure/Persistence/TaskFlowDbContextDomainEventsTests.cs` — EF Core
  InMemory + a fake `IPublisher` recording calls: confirms `TaskCompletedEvent` is published
  exactly once when a task reaches `Done`, not on other transitions, and that the entity's
  `DomainEvents` are cleared afterward.
- `tests/UnitTests/Application/Tasks/TaskCompletedEventHandlerTests.cs` — confirms the handler
  logs the task/board/assignee ids.
- `dotnet test` — 31 unit tests, 3 integration tests (real Postgres via Testcontainers), all
  passing; the integration suite exercises the real `TaskFlowDbContext` end-to-end.
