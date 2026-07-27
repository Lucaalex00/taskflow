# 2026-07-27 — Initial-based avatars with a user-chosen color, and a header user menu

## Context

Users were referenced by plain text ("Assigned to X", a bare name in the member list) with only
a tiny color dot. Avatars — a colored circle with the person's initial — are far more scannable,
and letting each person pick their own color (changeable any time) makes the board feel personal
and makes assignees recognisable at a glance.

## What changed

**Shared `AvatarComponent`**: a small standalone component that renders a circle with the first
initial of a name on a given color, in three sizes (sm/md/lg). Used everywhere a user appears.

**Editable color** (builds on the `PATCH /api/users/me/color` endpoint added earlier):
`CurrentUserService.updateColor` calls the endpoint and updates the locally-stored color signal,
so every avatar reflects the change immediately and it survives a reload (persisted server-side
*and* cached in `localStorage`).

**Shell-header user menu** (`UserMenuComponent`): the signed-in user's avatar now sits in the app
header on every page. Clicking it opens a small menu with a large avatar, the display name, a
native color picker ("Avatar color") that saves live, and "Sign out". Sign-out therefore moved
out of the board-list header (removed the now-redundant button and its method) into this
always-available menu.

**Avatars in place of dots/text**:
- Board-detail member list: `sm` avatar instead of the color dot.
- Task-card assignee: `sm` avatar + the assignee's name (replacing the "Assigned to X" text and
  dot); unassigned tasks read "Unassigned".

## Verification

- `dotnet test`: 119 unit + 29 integration (the color endpoint/handler/domain were covered in the
  previous commit).
- `ng test`: **95** (new `avatar.component.spec.ts` — initial derivation incl. empty/null/space
  fallback and color application; `user-menu.component.spec.ts` — toggle, save-color success and
  failure, sign-out; board-list sign-out test removed with the button). `ng lint` clean.
- E2E: all 6 Playwright tests green — added a `signOut` helper (opens the user menu, then clicks
  Sign out) and updated the assignee assertions to the new avatar-name markup. Ad-hoc checks:
  changing the color turns the header avatar green live and it stays green after a reload.
- Visual: screenshots confirm the header avatar, the assignee avatar on cards, the member-list
  avatars, and the user-menu popover with the color picker.
