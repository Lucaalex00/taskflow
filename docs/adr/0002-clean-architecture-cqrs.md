# 0002 — Clean Architecture + CQRS (via MediatR)

## Status
Accepted

## Context
The project's own principles call for production-grade structure that demonstrates real
engineering judgment to reviewers, not just working code. The domain also has real business
rules (task state machine, alert threshold evaluation) that deserve a home separate from
both persistence concerns and HTTP concerns.

## Decision
Split the backend into four projects — `Domain`, `Application`, `Infrastructure`, `Api` —
with dependencies pointing inward (`Api` → `Infrastructure` → `Application` → `Domain`).
Within `Application`, use CQRS: every use case is a `Command` or `Query` handled by a
single MediatR handler, with FluentValidation running automatically via a pipeline
behavior before the handler executes.

## Rationale
- **Testability without a database**: `Application` handlers depend only on the
  `ITaskFlowDbContext` abstraction, so unit tests (see `tests/UnitTests/Application`) can
  swap in an EF Core InMemory-backed fake with zero production code changes.
- **Business rules live in the Domain, not scattered in handlers**: `TaskItem.TransitionTo`
  enforces the entire state machine in one place (see the entity itself), so a handler can
  never accidentally allow an invalid transition — the compiler and the entity's own
  invariants prevent it.
- **One handler per use case** keeps each operation's logic in a single, short,
  independently testable file instead of a large service class accumulating every
  operation for an entity.
- **Validation is declarative and automatic**: `ValidationBehavior<TRequest, TResponse>`
  runs every registered `IValidator<T>` before a handler executes, so controllers never
  need manual `ModelState` checks.

## Alternatives considered
- **A single "fat" service layer** (e.g. `TaskService` with every task operation as a
  method): simpler for a small project, but doesn't scale past a handful of operations
  and tends to accumulate cross-cutting concerns inside one class. Rejected because
  demonstrating CQRS is itself part of the portfolio value of this project.
- **Minimal APIs with inline logic**: faster to write, but pushes business logic into
  the HTTP layer and makes unit testing harder without a real HTTP pipeline.

## Consequences
- More files per feature (a command, a validator, a handler) than a simpler service-based
  approach — an intentional trade-off documented here so it doesn't read as
  over-engineering to a reviewer.
- Adding a new use case always follows the same recipe: add a `Command`/`Query` record,
  a validator if needed, and a handler — consistent enough to onboard a new contributor
  quickly.
