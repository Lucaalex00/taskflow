# 2026-07-23 — Owner-only task creation, board ownership display, and a live-refresh bug fix

## Context

Follow-up feedback after trying the invitations/notifications/assignment work live:

1. Members could still **create** tasks — only assignment had been locked down to the Owner.
   A Member should be assigned to tasks by the Owner, not create their own.
2. Bug: accepting a board invitation from the notification bell didn't make the newly-joined
   board show up if the board list was already on screen — it required a manual page reload.
3. Nothing distinguished "a board you created" from "a board someone invited you to" in the UI.

## What changed

**Backend**:
- `CreateTaskCommandHandler`: `EnsureMemberAsync` → `EnsureOwnerAsync`. Members can still move
  tasks through the state machine (`TransitionTaskStateCommandHandler` is unchanged) and are
  still assigned to tasks by the Owner — they just don't create the work items themselves.
- `BoardDto` gained `OwnerDisplayName`, populated by joining `Users` in `GetBoardsQueryHandler`
  (an inner join — safe, since every board's owner is always a real registered user).

**Frontend**:
- `board-detail`: the "+ New task" button and its form only render for `isOwner()`.
- `board-list`: each board card shows "created by you" or "created by {ownerDisplayName}"
  (`isOwnBoard()` compares `board.ownerId` to the current user's id).
- **The refresh bug**: `BoardService` now holds a shared `boards` signal (the same pattern
  already used for `NotificationService.notifications`) instead of `BoardListComponent` keeping
  its own local copy. `BoardListComponent` reads `boardService.boards` directly, and
  `NotificationBellComponent` — which lives in the app shell and is mounted on every page —
  calls `boardService.refresh()` right after a successful invitation acceptance. Since the
  signal is shared, the board list reflects the newly-joined board immediately, wherever it's
  currently displayed, without needing to know the bell exists.

## Verification

- `dotnet test`: 104 unit (new: `CreateTaskCommandHandlerTests` Forbidden case,
  `GetBoardsQueryHandlerTests` owner-display-name case) + 16 integration (new:
  `Member_CannotCreateTasks`), all passing.
- `ng test`: 82, all passing (new: `board.service.spec.ts` `refresh()`, `board-list` ownership
  cases, `notification-bell` board-refresh-on-accept cases). `ng lint`: clean.
  `ng build --configuration production`: succeeds.
- Rebuilt the `api`/`frontend` containers and confirmed live: `GET /api/boards` now returns
  `ownerDisplayName`; a Member gets 403 creating a task while the Owner gets 201 for the same
  request.
