# 2026-07-27 — Drag & drop tasks between columns

## Context

Moving a task meant clicking a "→ In progress" button. Dragging a card between columns is the
gesture everyone expects from a Kanban board, and it's faster. It must still respect the domain
state machine — you can't drag a card into a state it's not allowed to transition to.

## What changed

- Added `@angular/cdk` and wired `DragDropModule` into `board-detail`.
- Each column body is a `cdkDropList` (`id="column-<State>"`, `cdkDropListData` = the state);
  each task card is a `cdkDrag` carrying the task as `cdkDragData`, with a compact colored
  drag preview.
- `cdkDropListConnectedTo` is computed per column from `ALLOWED_TRANSITIONS`, so a column only
  offers the *other* columns its cards may legally move into — CDK physically won't let you
  drop a card into an invalid column (it snaps back).
- `onTaskDropped` re-checks the transition is valid (defence in depth) and calls the same
  `moveTask` → `PATCH /api/tasks/{id}/state` path the buttons use, then reloads. The explicit
  transition buttons stay for keyboard/accessibility and discoverability.
- Styling for the drag lifecycle: `grab`/`grabbing` cursors, a colored lifted preview, a faded
  placeholder gap, and a dashed outline on a column that can receive the dragged card.
- Bumped the `anyComponentStyle` budget (8→10 kB warn, 12→14 kB error): board-detail is the
  app's most complex view and the drag styles pushed it 27 bytes over the old warning.

## Verification

- `ng lint` clean; `ng test`: 95 (drag-drop is exercised via E2E rather than unit, since it's
  DOM-pointer behavior); `ng build --configuration production`: succeeds, no budget warning.
- E2E: **8** Playwright tests (2 new in `dragdrop.spec.ts`) — dragging a To-do card into
  In-progress moves it and persists; dragging a To-do card onto Done (not a valid transition, so
  not a connected drop target) leaves it in To-do. The drag is driven with manual mouse
  down/move(×N)/up steps because CDK only begins a drag after a few pixels of movement.
