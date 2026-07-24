# 2026-07-24 — Secure the SignalR alerts hub with JWT auth and board membership checks

## Context

Every REST endpoint requires a JWT bearer token, but `AlertsHub` didn't — any WebSocket
client that knew (or guessed) a board's GUID could call `JoinBoard` and start receiving that
board's live alerts. Identified as a real, unauthenticated gap while reviewing what's left to
harden now that the rest of the app has real auth and RBAC.

## What changed

**Backend**:
- `AlertsHub` is now `[Authorize]` and takes `ITaskFlowDbContext` in its constructor.
  `JoinBoard` reads the caller's user id off `Context.User`'s `sub` claim and checks
  `BoardMembers` directly for that (boardId, userId) pair before adding the connection to the
  group — a non-member gets a `HubException`, mirroring the 403 the REST API already returns
  for the same scenario.
- `Program.cs`: browsers can't set an `Authorization` header on a WebSocket handshake, so
  SignalR's client sends the token as an `access_token` query string parameter instead. Added
  a `JwtBearerEvents.OnMessageReceived` handler that reads it from the query string, but only
  for requests under `/hubs` — REST endpoints still require a real header.

**Frontend**:
- `AlertService` now injects `CurrentUserService` and passes `accessTokenFactory: () =>
  this.currentUser.token() ?? ''` to `HubConnectionBuilder.withUrl`, so the hub connection
  authenticates the same way the REST calls already do via the auth interceptor.

## Verification

- `dotnet test`: 104 unit + **19 integration** (new `AlertsHubTests`, using a real
  `HubConnectionBuilder` client over the `WebApplicationFactory` TestServer, backed by
  `Microsoft.AspNetCore.SignalR.Client`):
  - a connection with no token is rejected at `StartAsync`
  - a board member can `JoinBoard` their own board without error
  - a non-member throws `HubException` trying to join someone else's board
- `ng test`: 82, all passing (`alert.service.spec.ts` already mocked `HubConnectionBuilder`
  fully, so no new test was needed there beyond confirming the existing suite still passes
  with the added constructor dependency).
