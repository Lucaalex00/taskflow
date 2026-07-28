# 2026-07-27 — New-task modal and hover polish

## Context

The "New task" form rendered inline at the top of the board, pushing the columns down and
shifting the layout every time it opened. A centered modal keeps the board stable and reads as
a clear, focused action. Plus general hover feedback so the board feels responsive.

## What changed

- The new-task form is now a centered **modal** with a dimmed backdrop (click outside or the ×
  to close), a titled header, placeholder hints in the fields, and explicit **Cancel** /
  **Create task** actions. Opening it no longer reflows the board.
- **Hover feedback**: task cards lift slightly and take on their state color + a soft shadow on
  hover (complementing the existing board-card and button hovers).

## Verification

- `ng lint` clean; `ng build --configuration production` succeeds (no budget warning);
  `ng test`: 104, all passing (markup/style change; the create-task flow — open form → fill
  Title → Create — is unchanged, so the existing specs and the collaboration/drag-drop E2E that
  create tasks through it still pass).
- E2E: all 8 Playwright tests green against the rebuilt stack.
- Visual: screenshot confirms the centered modal (title/description/priority/due + Cancel/Create)
  over a dimmed board.
