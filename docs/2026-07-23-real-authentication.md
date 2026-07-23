# 2026-07-23 — Real authentication (JWT)

## Context

`CurrentUserService` was explicitly demo-scope (see OVERVIEW.md "Future work"): no password,
just a generated id persisted to `localStorage`. The user asked for real permissions/roles,
which only makes sense on top of real accounts — so this is stage 1 of that work: password-based
registration and login issuing a JWT, with every existing endpoint now requiring it. Board-scoped
roles (Owner/Member) and colors are separate, later stages.

## What changed

**Domain** — `User` gains `PasswordHash` (an opaque string; Domain never sees a plaintext
password or hashing algorithm, per ADR 0002's zero-framework-dependencies rule). `User.Create`
now requires it.

**Application**:
- `Common/Interfaces/IPasswordHasher`, `IJwtTokenGenerator` — abstractions Infrastructure implements.
- `Common/Exceptions/AuthenticationException` — mapped to 401 in `ExceptionHandlingMiddleware`.
- `Users/AuthResult` (`UserId`, `DisplayName`, `Token`) — returned by both registration and login,
  so the frontend is signed in immediately after registering (no separate login round-trip).
- `CreateUserCommand` now takes a `Password`, hashes it, and checks email uniqueness explicitly
  (previously relied on the DB's unique index, which would have surfaced as an unhandled 500).
- `Users/Commands/Login` (new) — verifies credentials, throws `AuthenticationException` on
  any mismatch (deliberately the same message for "no such user" and "wrong password", to avoid
  leaking which emails are registered).

**Infrastructure**:
- `Services/PasswordHasher` — PBKDF2-HMAC-SHA256, random 16-byte salt, 100k iterations,
  constant-time comparison. Self-contained rather than ASP.NET Core Identity's
  `PasswordHasher<TUser>`, which needs a live `TUser` instance to hash against — awkward with
  Domain's private constructors.
- `Services/JwtTokenGenerator` — `System.IdentityModel.Tokens.Jwt`, HMAC-SHA256 signing,
  claims: `sub` (user id), `email`, `displayName`. Config-driven (`Jwt:Secret/Issuer/Audience/ExpiryMinutes`).
- `AddUserPasswordHash` EF migration.

**Api**:
- `AuthController` — `POST /api/auth/login`.
- `UsersController.Create` (register) — `[AllowAnonymous]`, now returns `AuthResult` instead of a bare id.
- Every other controller/endpoint (`Boards`, `Tasks`, `Alerts`, `Users.GetAll`) — `[Authorize]`.
- `Program.cs` — JWT bearer authentication wired up (`AddAuthentication().AddJwtBearer(...)`,
  `UseAuthentication()`/`UseAuthorization()` in the pipeline, before `MapControllers`).
- `appsettings.json` — a clearly-labeled dev-only signing secret (`Jwt:Secret`). Fine for this
  portfolio demo; call out `Jwt:Secret` as something a real deployment would inject via a secret
  store, not commit.

**Frontend**:
- `core/models/user.model.ts` — `CreateUserRequest` gains `password`; new `LoginRequest`, `AuthResult`.
- `CurrentUserService` — now holds a `token` signal (persisted to `localStorage`) alongside
  `userId`/`displayName`; `register()`/`login()` both call `applyAuthResult`; `isAuthenticated`
  replaces the old `isRegistered`.
- `core/interceptors/auth.interceptor.ts` — attaches `Authorization: Bearer <token>` to every
  request; on a 401 response, signs out and redirects to `/login` (handles token expiry).
- `core/guards/auth.guard.ts` — redirects to `/login` unless `isAuthenticated()`.
- `features/auth/login/` (new) — single component, Sign-in/Register tabs, replaces the inline
  onboarding form that used to live in `board-list.component`.
- `app.routes.ts` — `/login` is public; `/` and `/boards/:id` are guarded.

## Known gap (accepted for this stage)

`AlertsHub` (SignalR) still accepts anonymous connections — wiring JWT auth over the WebSocket
handshake (reading the token from the query string, since browsers can't set custom headers on
the initial upgrade request) is a separate, self-contained chunk of work, deferred so this stage
stays reviewable. Board-scoped authorization (who can see/act on which board) is stage 2.

## Migrating existing demo data

Any users created before this change have an empty `PasswordHash` and can no longer log in
(by design — there's no password to check against). They'll need to re-register, or wipe the
dev Postgres volume (`docker compose down -v`) for a clean slate.

## Verification

- `dotnet test`: 44 unit (new: `PasswordHasherTests`, `JwtTokenGeneratorTests`,
  `CreateUserCommandHandlerTests`, `LoginCommandHandlerTests`) + 4 integration
  (new: `BoardEndpoint_WithoutAuthentication_Returns401`), all passing.
- `ng test`: 56 (new: `current-user.service.spec` rewritten for tokens, `auth.interceptor.spec`,
  `auth.guard.spec`, `login.component.spec`), all passing. `ng lint`: clean. `ng build --configuration production`: succeeds.
- Rebuilt the `api`/`frontend` containers and smoke-tested register → login → an authenticated
  call against the running stack.
