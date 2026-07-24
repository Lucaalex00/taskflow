# 2026-07-24 — Fixed violet/pink theme and a redesigned notification side drawer

## Context

Two UX asks: (1) commit to a single, cohesive dark theme built on violet + pink rather than
the previous teal accent, and manage colors from one place; (2) the notification bell's small
dropdown was cramped and hard to use — replace it with something clearer where you can tell at
a glance whether something is an invitation vs. a task update, and which items are new vs.
already read.

## What changed

**Theme** (`styles.scss`, one place):
- Re-centered the palette on a deep, slightly violet-tinted dark base (`--color-bg: #0f0d17`)
  with a violet primary (`--color-accent: #a855f7`) and a pink secondary
  (`--color-accent-2: #ec4899`), plus dimmed variants for chip/badge backgrounds.
- Primary buttons now use a violet→pink gradient with white text (was flat teal with dark text).
- `board-list` default board color moved from teal to violet.
- `ColorPalette` (Domain) re-curated to eight violet/pink/indigo-family colors, so
  auto-assigned user avatar and board colors harmonize with the theme while staying
  distinguishable from each other. (No test change needed — the palette tests only assert hex
  format and determinism, not specific values.)

**Notification drawer** (`notification-bell` component):
- The dropdown is now a right-side **drawer** with a backdrop, sliding in over the page —
  more room, and it reads like a real inbox.
- Each notification carries a **type chip** and a colored **left accent bar**, tone-coded:
  invitations pink, task assignments violet, state changes indigo. So the kind of notification
  is obvious before reading the text.
- Clear **read/unread**: unread items are full-strength with a colored bar, a "new" dot, and a
  per-item "Mark read"; read items visibly recede (dimmed). A **"Mark all read"** action and an
  unread **"N new"** pill sit in the header, and there's a friendly empty state.

## Verification

- `ng test`: **87 frontend** (updated `notification-bell.component.spec.ts` for the renamed
  drawer controls `openDrawer`/`closeDrawer`, new `typeMeta` tone mapping, and `markAllRead`;
  the service gained a `markAllRead` that marks every unread item read). `ng lint`: clean (the
  backdrop is a real focusable `<button>` for keyboard accessibility).
- E2E: all 6 Playwright tests green against the rebuilt stack; `collaboration.spec.ts` updated
  to close the drawer via its close button (the trigger now only opens, it no longer toggles).
- Visual: captured screenshots of the themed board list, board detail, and a populated drawer
  (pink "INVITATION" chip + accent bar, "1 new" pill, unread dot, gradient Accept button) to
  confirm the design renders as intended in the real Docker build.
