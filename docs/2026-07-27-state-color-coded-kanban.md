# 2026-07-27 — Traffic-light color coding for the Kanban board

## Context

The board columns were all the same neutral grey, so which column a task sat in carried no
instant visual meaning — you had to read the header. People already associate board states
with traffic-light colors (grey "to do", amber "in progress", red "blocked", green "done"), and
leaning into that convention makes the board readable at a glance. First step of a broader
usability pass.

## What changed

**Theme tokens** (`styles.scss`): added a dedicated set of task-state colors —
`--state-todo` (slate), `--state-inprogress` (amber), `--state-blocked` (red),
`--state-done` (green), each with a `-dim` tint. Kept deliberately separate from the
violet/pink brand accents: brand color is for actions/identity, state color is semantic and
should read the same way it does in every other tracker.

**Kanban columns** (`board-detail`): each column carries a `data-state` and drives a local
`--state-color`/`--state-tint` CSS variable, so one small block of CSS colors:
- a colored **top border** on the column,
- a **tinted header** with a colored **swatch dot** and colored, uppercased label,
- a **pill count badge** tinted to the state color.

**Task cards**: a `data-state` sets the card's **left accent bar** to its state color
(overdue still overrides to red — an at-risk task should shout regardless of column).

**Transition buttons**: each `→ {state}` button hints its *target* state's color on hover, so
"→ Done" reads green and "→ Blocked" reads red before you read the label.

## Verification

- `ng lint` clean; `ng test`: 87, all passing (pure styling/markup change, no behavior touched);
  `ng build --configuration production`: succeeds, no component-style budget warning.
- E2E: all 6 Playwright tests green against the rebuilt stack.
- Visual: captured a board with tasks spread across all four columns (and every priority) to
  confirm the traffic-light coding renders as intended — grey/amber/red/green columns, matching
  card accent bars, tinted count badges.
