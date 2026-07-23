# 0003 — SignalR for real-time alerts

## Status
Accepted

## Context
The core feature of TaskFlow is workload anomaly detection: a background worker
periodically evaluates alert rules and needs to notify whoever is looking at a board the
moment something is raised, without the client having to poll the API repeatedly.

## Decision
Use ASP.NET Core SignalR. Clients connect to `AlertsHub` and join a group named after the
board id they're viewing (`JoinBoard(boardId)`); the worker broadcasts to that group via
`IHubContext<AlertsHub>` through the `SignalRAlertNotifier` (implementing the
Application-layer `IAlertNotifier` interface, so Application has zero knowledge of SignalR
as a transport).

## Rationale
- **Native to ASP.NET Core**: no extra infrastructure (message broker, external pub/sub
  service) is needed — it runs in-process, which fits the "zero fixed cost" constraint.
- **Automatic reconnection and fallback**: SignalR degrades from WebSockets to Server-Sent
  Events to long polling automatically if a network doesn't support WebSockets, which
  matters for a demo that might run behind different browsers/networks/corporate proxies.
- **Group-based targeting fits the domain exactly**: alerts are board-scoped, and
  SignalR groups are a first-class primitive for "broadcast to a subset of connections" —
  no need to track connection-to-board mappings manually.
- **Kept out of Application layer**: the `IAlertNotifier` abstraction means the worker's
  business logic (which rule fired, for whom) has no dependency on SignalR types; a future
  swap to another transport would only touch `Infrastructure/Realtime`.

## Alternatives considered
- **Client-side polling** (e.g. `GET /boards/{id}/alerts` every N seconds): simplest to
  build, but either wastes requests when nothing changed or adds latency waiting for the
  next poll. Explicitly against the brief's ask for "real-time monitoring."
- **A separate message broker** (RabbitMQ, Azure Service Bus): overkill for a single-instance
  demo deployment and violates the zero-fixed-cost constraint for anything beyond a
  free tier.

## Consequences
- The Angular client must explicitly join/leave a board's group when navigating
  (`AlertService.connectToBoard` / `disconnect`), or it will miss alerts for the board
  it's viewing.
- SignalR requires WebSocket-friendly infrastructure end-to-end; the nginx config
  (`docker/nginx.conf`) explicitly forwards `Upgrade`/`Connection` headers for the
  `/hubs/` path so the Docker Compose demo works without extra client configuration.
