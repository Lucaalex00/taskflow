# 2026-07-23 — Board-scoped roles (Owner/Member)

## Context

Stage 2 of the requested permissions/roles work (stage 1 was real authentication). Before
this, there was no membership concept at all: every authenticated user could see every board,
create tasks on any board, and assign any task to any registered user. This adds real
per-board access control: a board has Owner and Member roles, only members can see/act on a
board, and only Owners manage its membership.

## What changed

**Domain**: `BoardRole` enum (`Owner`, `Member`); `BoardMember` entity (`BoardId`, `UserId`,
`Role`, `JoinedAtUtc`).

**Application**:
- `ICurrentUserService` (`UserId`) — abstraction over "who is making this request"; Application
  depends on it, not on ASP.NET Core's `ClaimsPrincipal`.
- `IBoardAuthorizer` / `BoardAuthorizer` — `EnsureMemberAsync`/`EnsureOwnerAsync`, throwing
  `ForbiddenException` (mapped to 403) otherwise. One place for the "is this user allowed here"
  check, used consistently across every board-scoped handler instead of duplicating the query.
- `CreateBoardCommand` no longer takes an `OwnerId` — it's derived from `ICurrentUserService`
  and the creator is added as the board's first `BoardMember` (`Owner`) in the same transaction.
  (Previously, `OwnerId` was a client-supplied field with no check that it matched the caller —
  a latent hole this closes as a side effect.)
- `GetBoardsQuery` now filters to boards the caller is a member of, instead of listing every
  board system-wide.
- `AssignTaskCommand` now additionally requires the assignee to be a member of the task's board
  (previously any registered user, board-wide or not, could be assigned).
- Every board-scoped handler (`CreateTask`, `GetBoardTasks`, `TransitionTaskState`, `AssignTask`,
  `GetBoardAlerts`, `MarkAlertRead`) calls `EnsureMemberAsync` for the relevant board;
  `CreateAlertRule` calls `EnsureOwnerAsync` (rule thresholds affect the whole board, so
  configuring them is an Owner action).
- New: `GetBoardMembersQuery`, `AddBoardMemberCommand` (Owner-only; rejects duplicates),
  `RemoveBoardMemberCommand` (Owner-only; refuses to remove a board's last Owner).

**Infrastructure**: `CurrentUserService : ICurrentUserService` reads the `sub` claim off the
current `HttpContext.User` (via `IHttpContextAccessor`). `Program.cs` sets
`options.MapInboundClaims = false` on the JWT bearer handler so `sub` isn't silently renamed to
`ClaimTypes.NameIdentifier` (ASP.NET Core's default, easy to trip over) — `JwtTokenGenerator`
and `CurrentUserService` need to agree on the same claim name. `AddBoardMembers` EF migration.

**Api**: `GET/POST /api/boards/{id}/members`, `DELETE /api/boards/{id}/members/{userId}`.

**Frontend**:
- `board.model.ts` — `BoardRole`, `BoardMemberDto`, `AddBoardMemberRequest`; `CreateBoardRequest`
  drops `ownerId` (server derives it now).
- `BoardService` — `getMembers`/`addMember`/`removeMember`.
- `board-detail.component` — the assignee dropdown is now fed by the board's members (not every
  registered user); a collapsible members panel lists members + roles, and — only when
  `isOwner()` — an "add member" form (picking from registered users not yet on the board) and a
  remove button per member.

## Bug caught by the real-Postgres integration tests (not the InMemory unit tests)

`GetBoardMembersQueryHandler` originally did `.Join(...).Select(m => new BoardMemberDto(...))
.OrderBy(m => m.DisplayName)` — ordering by a property of an already-constructed record. EF
Core's InMemory provider evaluates this client-side without complaint, but the real Npgsql
provider throws `InvalidOperationException: could not be translated`. Fixed by ordering on the
join's intermediate anonymous projection, before the `Select` into the DTO. This is exactly the
kind of thing the project's two-tier test strategy (fast InMemory unit tests + real-Postgres
integration tests) exists to catch — the enum-serialization bug from the previous stage was a
mirror-image case (client/server agreement, not query translation).

## Known gap

No UI/API to rename or delete a board, or to change an existing member's role in place (remove
+ re-add works). Not part of what was asked; left out to keep this stage reviewable.

## Migrating existing demo data

Boards created before this change have no `BoardMember` rows, so they'll be invisible to
everyone (not deleted, just inaccessible via `GetBoardsQuery`) until wiped
(`docker compose down -v`) or manually backfilled.

## Verification

- `dotnet test`: 58 unit (new: `BoardAuthorizerTests`, `CreateBoardCommandHandlerTests`,
  `GetBoardsQueryHandlerTests`, `AddBoardMemberCommandHandlerTests`,
  `RemoveBoardMemberCommandHandlerTests`, `GetBoardMembersQueryHandlerTests`,
  `AssignTaskCommandHandlerTests`) + 9 integration (new: `BoardMembersEndpointsTests` — board
  isolation between users, owner-only add/remove, last-owner protection), all passing.
- `ng test`: 67, all passing. `ng lint`: clean. `ng build --configuration production`: succeeds.
- Rebuilt the `api`/`frontend` containers and smoke-tested live: board isolation (an outsider
  gets 403 on a board's tasks), member listing, all against the running stack.
