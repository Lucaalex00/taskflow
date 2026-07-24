# 2026-07-24 — Stronger password policy, mirrored live in the UI

## Context

Registration only required a password of 8+ characters — no complexity at all. For a project
meant to show real auth/security awareness, that's weak, and the UI gave no hint of the
requirement until the server rejected a too-short password with a 400.

## What changed

**Backend** — a single source of truth, `PasswordRules.Password()`
(`Application/Common/Validation`), a reusable FluentValidation rule requiring: ≥10 characters,
at least one uppercase, one lowercase, and one number, each with its own message.
`CreateUserCommandValidator` uses it. `LoginCommandValidator` deliberately does **not** — login
only checks the password is non-empty, so tightening the policy never locks existing users out
and never leaks the policy to an attacker probing the login endpoint.

**Frontend** — `LoginComponent` now holds the password in a signal and derives a live
`passwordRequirements()` checklist (same four rules) plus `isPasswordValid()`. In register
mode the login form shows the checklist, each item turning from muted "○" to accent "✓" as it's
satisfied, and the submit button is disabled until all four pass — so the user sees exactly
what's needed while typing instead of discovering it via a server error. Login mode is
unaffected (no checklist, no gating).

## Verification

- `dotnet test`: **109 unit** (new `CreateUserCommandValidatorTests`: one strong password
  passes, four weak variants each fail on the expected rule) + 24 integration.
- `ng test`: **85 frontend** (new `login.component.spec.ts` cases: `isPasswordValid` tracks the
  policy, and register-mode submit is blocked when the password fails it). `ng lint`: clean.
- E2E: all 6 Playwright tests green — the shared test password was updated to satisfy the new
  policy, and the form selectors switched from the (now dynamic) password placeholder to the
  stable `input[name="password"]`.
- Note: the previous demo/test password `correct-horse-battery-staple` no longer satisfies the
  policy (no uppercase, no number); every test fixture and the README curl examples were updated
  to `Correct-horse-battery-staple9`.
