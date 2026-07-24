# 2026-07-24 — Rate limit login/register against brute-force attempts

## Context

`/api/auth/login` and `/api/users` (registration) are the only two endpoints that don't
require an existing JWT — by design, since you need one of them to get a token in the first
place. That also makes them the only realistic target for a credential-stuffing or
brute-force script, since every other endpoint is already gated by authentication.

## What changed

- `Program.cs`: `AddRateLimiter` with a per-client-IP fixed-window policy named `"auth"` —
  default 10 requests/minute, configurable via `RateLimiting:Auth:PermitLimit` /
  `WindowSeconds` in `appsettings.json`. Exceeding it returns 429 with no queueing
  (`QueueLimit = 0` — reject immediately rather than making the caller wait).
- `AuthController.Login` and `UsersController.Create` are annotated `[EnableRateLimiting("auth")]`.
- The limiter reads its configuration via `httpContext.RequestServices.GetRequiredService<IConfiguration>()`
  inside the policy factory, rather than capturing a value from `builder.Configuration` at
  startup — the latter doesn't see configuration overrides that `WebApplicationFactory` adds
  in tests (that override is only visible through the DI-resolved `IConfiguration`, built
  after `builder.Build()`). Same reasoning is why the actual Postgres connection string
  resolves correctly in integration tests despite `ConnectionStrings:Postgres` also being read
  at the top of `Program.cs` — anything that needs to see a `WebApplicationFactory` override
  must be resolved from DI, not captured into a local variable pre-`Build()`.

## Verification

- `dotnet test`: 104 unit + **20 integration**:
  - `TaskFlowApiFactory` (used by every other integration test class) sets
    `RateLimiting:Auth:PermitLimit=1000` so the existing suites, which register far more users
    per minute than a real client would, aren't throttled.
  - New `RateLimitedApiFactory` keeps the real default limit and a new
    `AuthRateLimitingTests.Register_BeyondTheConfiguredLimit_Returns429` fires 5 requests
    against a 3/minute limit and asserts the last one is `429 Too Many Requests`.
