# 2026-07-27 — Board search and filters

## Context

Once a board has more than a handful of tasks, finding a specific one — or focusing on one
person's work, or just the high-priority items — means scanning every column. A small filter
bar makes the board usable at scale.

## What changed

A filter toolbar above the Kanban columns in `board-detail`, all client-side over the already-
loaded tasks (signals, no extra requests):
- **Search** — matches task title *or* description, case-insensitive.
- **Assignee** — All / Unassigned / each board member.
- **Priority** — All / Low / Medium / High / Critical.

The three filters combine (AND). `filteredTasks` is a computed that feeds the existing
`tasksByColumn`, so columns, counts, and drag & drop all operate on the filtered set with no
other changes. When any filter is active, a "Clear filters" button and an "N shown" count
appear.

## Verification

- `ng lint` clean; `ng build --configuration production` succeeds (no budget warning);
  `ng test`: **99** — four new `board-detail.component.spec.ts` cases: search matches
  title/description, assignee filter incl. the "unassigned" option, priority filter, and
  `clearFilters` resetting everything (and `hasActiveFilters`).
