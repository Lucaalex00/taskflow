# 2026-07-28 — Close-confirmation, locked Done cards, logical delete (archive), and a filter dropdown

## Context

A batch of board-usability requests: closing a task (moving it to Done) should be a deliberate
step with a confirmation; a closed task should be locked (no longer draggable) and offer a way
to remove it that keeps the record in the database (a logical delete); those removed tasks
should be viewable on demand; and the always-visible filter selects should collapse into a
cleaner dropdown behind a "Filter" control.

## What changed

**Backend — task archive (logical delete):**
- `TaskItem` gained `ArchivedAtUtc` / `IsArchived` and an `Archive(now)` method (idempotent;
  only a Done task can be archived — archiving is the "close and file away" step). Migration
  `AddTaskArchive`.
- `GetBoardTasksQuery` takes `IncludeArchived` (default false) and the list endpoint an
  `includeArchived` query param, so archived tasks are hidden unless asked for. `TaskDto`
  carries `IsArchived`.
- New `ArchiveTaskCommand` + `PATCH /api/tasks/{id}/archive`, **Owner-only** (logical delete is
  a board-management action).

**Frontend:**
- **Close confirmation**: every move now goes through `requestMove`. Moving to Done (via button
  or drag) opens an "Are you sure you want to close this task?" dialog; confirm actually moves
  it. Other transitions are immediate.
- **Locked Done cards**: Done (and archived) cards are `cdkDragDisabled` and lose their hover
  lift — they read as finished. Done cards have no transition buttons (Done is terminal anyway).
- **Archive X**: an owner-only red **×** on a completed card logically deletes it (calls the
  archive endpoint), then it disappears from the board with a toast.
- **Show completed toggle**: a "Show completed (archived) tasks" checkbox re-fetches with
  `includeArchived`, bringing archived cards back — greyed, tagged "archived", read-only.
- **Filter dropdown**: the assignee/priority selects moved out of the toolbar into a panel
  revealed by a **Filter** button inside the search bar (with an active-filter count badge); the
  bar now shows just the search field, the Filter button, and an "N shown" count.

## Verification

- `dotnet test`: **127 unit** (new: `TaskItem.Archive` domain cases, `ArchiveTaskCommandHandler`)
  + **31 integration** (new `TaskArchiveEndpointTests`: archiving a Done task hides it unless
  `includeArchived=true`, and archiving a non-Done task is a 400).
- `ng test`: **114** (new board-detail cases for `requestMove`→confirm, `confirmMoveToDone`,
  `cancelMoveToDone`, `archiveTask`, `toggleShowArchived`, and the updated `task.service` URL
  with the `includeArchived` param). `ng lint` clean; production build succeeds (component-style
  budget bumped to 14 kB warn / 18 kB error — board-detail is the app's main view).
- E2E: **8** Playwright tests (new `close-archive.spec.ts`: move→confirm dialog→locked Done card
  with an archive ×→archive hides it→"show completed" brings it back tagged "archived").
- Visual: screenshots of the confirmation dialog, the locked Done card with the ×, and the
  filter dropdown.
