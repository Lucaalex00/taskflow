# 2026-07-27 — Toast notifications and clearer empty states

## Context

Actions like creating, moving, or assigning a task gave no confirmation — the board just
changed. A brief toast makes it clear the action succeeded (or failed). And empty Kanban columns
showed a bare "—", which reads as broken rather than intentional.

## What changed

**Toasts**: a small `ToastService` (a signal of active toasts with `success`/`error`/`info` and
auto-dismiss after 3.5s) plus a `ToastContainerComponent` mounted once in the app shell, showing
a colour-coded stack top-right (green success, red error, violet info) with a manual close.
Wired into the board's ephemeral actions:
- create task → "Task "…" created."
- move task (button or drag) → "Moved "…" to <column>." / error toast on failure.
- assign task → "Assigned "…" to <name>." / error toast on failure.

Form-level errors that belong next to their input (invite, role change, remove member, load
failure, task-create validation) keep their inline messages.

**Empty states**: empty columns now read "No tasks", or "No matches" when a filter is active,
instead of "—".

## Verification

- `ng lint` clean; `ng build --configuration production` succeeds; `ng test`: **104** — new
  `toast.service.spec.ts` (type/message, unique ids, dismiss, fakeAsync auto-dismiss); the
  board-detail move/assign tests updated to assert the success/error toasts (the component now
  routes those through the ToastService, which the spec provides as a spy).
