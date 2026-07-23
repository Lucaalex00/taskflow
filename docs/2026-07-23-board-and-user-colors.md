# 2026-07-23 — Board and user colors

## Context

Stage 3 (last) of "gestisci permessi, ruoli, nomi utente reali con le board, colori e roba" —
after real auth (stage 1) and board-scoped roles (stage 2), this adds a color per board (chosen
at creation) and per user (assigned automatically), used to tell boards/people apart at a
glance.

## What changed

**Domain**: `TaskFlow.Domain.Common.ColorPalette` — a curated 8-color palette (plain strings +
a regex validator, no external dependency, consistent with Domain's zero-dependency rule):
`PickRandom()`, `PickFor(Guid)` (deterministic — same id always maps to the same color),
`IsValidHex(string)`.
- `User.Color` — assigned in the private constructor via `ColorPalette.PickFor(Id)`. Not
  user-choosable; this is deliberately automatic (an avatar accent, not a preference to manage).
- `ProjectBoard.Color` — `Create(name, ownerId, color = null)`: if the caller passes a color,
  it's validated (`ColorPalette.IsValidHex`) and used; if omitted, `ColorPalette.PickRandom()`
  fills in. Existing 2-argument `Create` calls throughout the codebase kept compiling unchanged
  thanks to the optional parameter.

**Application**: `Color` added to `BoardDto`, `UserDto`, `BoardMemberDto`, `AuthResult` (so the
frontend gets the signed-in user's color immediately on register/login, no extra round-trip);
`CreateBoardCommand` gained an optional `Color`.

**Infrastructure**: `Color` columns (`varchar(7)`, `NOT NULL`) on `users` and `project_boards`;
`AddColors` EF migration.

**Frontend**:
- `core/constants/color-palette.ts` — mirrors the backend's 8 colors, offered as swatches.
- `CreateBoardRequest.color`, `BoardDto.color`, `UserDto.color`, `BoardMemberDto.color`,
  `AuthResult.color`.
- `CurrentUserService` persists the signed-in user's color alongside their id/name/token.
- `board-list`: a small circular swatch picker in the create-board form; each board card gets a
  4px left border in its color.
- `board-detail`: the members panel shows a colored dot next to each member's name; the
  assignee label on a task card shows a colored dot for the current assignee (via new
  `assigneeColor(task)`, mirroring the existing `assigneeName(task)`).

## Verification

- `dotnet test`: 71 unit (new: `ColorPaletteTests`, `ProjectBoardTests`, `User.Create` color
  assertion, `CreateBoardCommandHandlerTests` color case) + 9 integration, all passing.
- `ng test`: 68, all passing (every DTO test fixture updated for the new required `color`
  field). `ng lint`: clean. `ng build --configuration production`: succeeds.
- Rebuilt the `api`/`frontend` containers and smoke-tested live: a freshly registered user gets
  an automatic color (`#b794f4` in the run tested), a board created with an explicit color
  (`#f687b3`) round-trips correctly through `GET /api/boards`.

## Note

This closes out the three-stage "permissions/roles/colors" request:
1. [`2026-07-23-real-authentication.md`](2026-07-23-real-authentication.md)
2. [`2026-07-23-board-membership-and-roles.md`](2026-07-23-board-membership-and-roles.md)
3. This document.
