# 2026-07-23 — Assign tasks to any user from the UI

## Context

`TaskFlow.Api`'s `PATCH /api/tasks/{id}/assignee` already accepted any valid `userId`, but the
Angular UI only ever called it with the current (demo-scope) user's own id — "Assign to me"
was the only affordance. There was also no board-membership concept: a task can be assigned to
any registered user, board-wide, so the natural fix was a way to pick from every registered user.

## What changed

**Backend** — new read endpoint, following the same CQRS query pattern as `GetBoards`:
- `Application/Users/UserDto.cs`, `Application/Users/Queries/GetUsers/` (`GetUsersQuery` +
  handler) — lists every user ordered by display name.
- `Api/Controllers/UsersController.cs` — `GET /api/users`.

**Frontend**:
- `core/models/user.model.ts` — added `UserDto`.
- `core/services/user.service.ts` — thin `getAll()` wrapper, same shape as `BoardService`.
- `features/boards/board-detail/board-detail.component.ts` — loads the user list alongside
  tasks/alerts on init (best-effort: a failed load just leaves the dropdown at "unassigned",
  it doesn't block the board from rendering); added `assignTo(task, userId)` (used by both the
  new dropdown and the existing "Assign to me" shortcut, which now delegates to it) and
  `assigneeName(task)` to resolve a display name for the currently assigned user.
- Each task card now shows a "— choose assignee —" dropdown populated from `GET /api/users`,
  alongside the existing "Assign to me" quick action (kept for the common case).

## Why a new `GET /api/users` endpoint instead of a client-side user search

Considered letting the UI resolve a user by email instead of listing everyone, to avoid adding
a full user directory endpoint. Decided against it: this is a demo-scope app with no auth and
no board membership, so a global user list carries no real exposure it doesn't already have via
existing endpoints, and it gives a much better dropdown-driven demo experience than typing a
raw email or GUID.

## Verification

- `tests/UnitTests/Application/Users/GetUsersQueryHandlerTests.cs` — ordering and empty-list cases.
- `frontend/.../user.service.spec.ts` — HTTP contract.
- `frontend/.../board-detail.component.spec.ts` — user list loading (including a failed-load
  case), `assignTo`, `assigneeName` resolution (known/unknown/unassigned).
- `dotnet test`: 33 unit + 3 integration passing. `ng test`: 45 passing. `ng lint`: clean.
  `ng build --configuration production`: succeeds.
