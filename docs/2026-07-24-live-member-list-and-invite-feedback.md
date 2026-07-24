# 2026-07-24 — Live-refresh the board member list, and confirm invites explicitly

## Context

Writing the Playwright E2E suite surfaced a real (if minor) UX gap: `BoardDetailComponent`
fetches its member list once on load and never refreshes it. If the owner has a board open,
invites someone, and that person accepts without the owner reloading, the newly-accepted
teammate genuinely doesn't appear in the assignee dropdown — the owner has to refresh the page
to assign them a task. Separately, inviting someone gave no feedback beyond the form silently
clearing, which reads as "did that actually work?"

## What changed

- `BoardDetailComponent` now polls the member list every 20 seconds — the same interval and
  pattern `NotificationBellComponent` already uses for the same class of problem (no push
  channel for membership changes). `ngOnDestroy` clears the interval.
- `inviteMember()` now sets an `inviteSuccessMessage` signal ("Invitation sent to
  {email}.") on success, shown under the invite form in the members panel.

## Verification

- `ng test`: 83, all passing — new `board-detail.component.spec.ts` cases:
  `polls the member list every 20s...` (fakeAsync/tick, mirroring
  `notification-bell.component.spec.ts`'s existing polling test, confirms the poll fires and
  stops after `ngOnDestroy`), and an updated invite test asserting the confirmation message.
- `npx playwright test` (e2e/): all 6 still pass against the real Docker stack; re-ran several
  times to confirm no new flakiness from the added polling.
