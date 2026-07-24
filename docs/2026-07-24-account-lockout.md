# 2026-07-24 — Temporary account lockout after repeated failed logins

## Context

The per-IP rate limiter (see
[`docs/2026-07-24-rate-limit-login-and-register.md`](docs/2026-07-24-rate-limit-login-and-register.md))
throttles login attempts from a single address — but an attacker rotating IPs (a botnet, a
proxy pool) could still grind away at one account's password. A per-*account* lockout closes
that gap: too many consecutive failures for a given account locks it briefly, regardless of
where the attempts come from.

## What changed

**Domain** (`User`): added `FailedLoginAttempts` and `LockoutEndUtc`, with the lockout logic
encapsulated on the entity (not leaked into the handler):
- `RegisterFailedLogin(now, maxAttempts, lockoutDuration)` — increments the counter; on
  reaching `maxAttempts` it sets `LockoutEndUtc = now + duration` and resets the counter so the
  next window starts fresh.
- `RegisterSuccessfulLogin()` — clears the counter and any lockout.
- `IsLockedOut(now)` — true while the window is active.

**Application** (`LoginCommandHandler`): now takes `IDateTimeProvider`. It refuses login with a
distinct "account is temporarily locked" message while `IsLockedOut` is true (before even
checking the password), records a failed attempt on a wrong password, and clears the counter on
success. Policy: **5** consecutive failures → **15-minute** lockout. `LoginCommandValidator`
still only checks non-empty, so this never leaks the policy at the validation layer.

**Infrastructure**: `UserConfiguration` maps the two new columns (`FailedLoginAttempts` with a
`0` default, nullable `LockoutEndUtc`); migration `AddUserLockout` adds them. Applies
automatically on API startup like every other migration.

**Frontend** (`LoginComponent`): the login error handler now surfaces the server's lockout
message verbatim (a 401 whose ProblemDetails `detail` mentions "locked"), so the user
understands it's a temporary lock rather than wrong credentials. All other failures stay
intentionally vague ("Invalid email or password.").

## Verification

- `dotnet test`: **112 unit** (new `LoginCommandHandlerTests`: lockout after 5 failures blocks
  even the correct password; the window expiring re-allows login via a `FakeDateTimeProvider`
  advanced past 15 min; a success resets the counter) + **26 integration** (new
  `AccountLockoutTests`: full HTTP flow — 5 failed logins then the correct password returns 401
  with a "locked" body; 4 failures then correct still succeeds).
- `ng test`: 85, `ng lint` clean.
- Live (`docker compose up`, fresh DB so the migration applies from scratch): registered a
  user, sent 5 wrong logins (all 401), then the correct password → 401 with
  `"detail": "This account is temporarily locked after too many failed login attempts..."`.
