# 2026-07-23 — Invitations, notifications, and owner-only task assignment

## Context

The previous board-membership stage let an Owner add any registered user directly (picked
from a dropdown) and let any board member assign tasks to anyone, including themselves. That's
loose enough to make the tool feel like a personal to-do list rather than something with real
team structure. This stage makes membership and assignment mean something:

- Joining a board requires **consent** — an Owner invites by email, the invitee gets a
  notification and must accept (always joining as Member; promotion to Owner is a separate,
  later action).
- **Only the board Owner assigns tasks.** A Member can't assign to themselves or anyone else —
  but they can still move their own assigned tasks through the state machine (Todo → In
  progress → Done...), since blocking that would make the board unusable for whoever's actually
  doing the work.
- A lightweight **in-app notification center**: invitations, "you were assigned a task", and
  "your task's state changed" — but never for your own actions (moving your own task doesn't
  notify you).

## What changed

**Domain**:
- `InvitationStatus` (Pending/Accepted/Declined), `NotificationType`
  (BoardInvitation/TaskAssigned/TaskStateChanged).
- `BoardInvitation` — `BoardId`, `InviteeEmail`, `InviteeUserId` (nullable — see below),
  `InvitedByUserId`, `Status`. `Accept()`/`Decline()` guard against responding twice.
  `LinkInvitee(userId)` connects a since-registered user to an invitation that predates their
  account.
- `Notification` — `RecipientUserId`, `Type`, `Message`, optional `BoardId`/`TaskId`/
  `InvitationId` (the last one only set for `BoardInvitation` notifications, so the UI knows
  which invitation to Accept/Decline).
- `BoardMember.ChangeRole(newRole)` — in-place role change (previously the only way to change
  a member's role was remove + re-add).

**Application**:
- `InviteBoardMemberCommand` (Owner-only) — looks up the invitee by email. If they're already
  registered, creates the invitation **and** a notification immediately. If not, the invitation
  is stored with `InviteeUserId = null` and no notification yet — nothing to notify.
- `CreateUserCommandHandler` now also links any pending invitations matching the new user's
  email (`InviteeUserId` was null) and creates the notification at that point. This is what
  makes "invite an email that isn't registered yet" work — the notification simply waits.
- `RespondToBoardInvitationCommand` — only the invitee can respond; accepting creates a
  `BoardMember` as **Member** (never Owner, regardless of who invited); either response marks
  the triggering notification read.
- `UpdateBoardMemberRoleCommand` (Owner-only) — the same last-owner protection as removing a
  member applies when demoting one.
- `AssignTaskCommandHandler` — `EnsureMemberAsync` → `EnsureOwnerAsync`. Also creates a
  `TaskAssigned` notification for the assignee, skipped when assigning to yourself.
- `TransitionTaskStateCommandHandler` — unchanged authorization (any member), but now creates a
  `TaskStateChanged` notification for the task's assignee, skipped when the assignee is the one
  making the change.
- `GetNotificationsQuery` — left-joins `BoardInvitation` so each notification carries the
  invitation's live `Status` (a `BoardInvitation` notification for an invitation already
  responded to shows the outcome instead of Accept/Decline).
- `AddBoardMemberCommand` (direct add) is gone — invitations are the only way in now.

**Infrastructure**: EF configs + `AddInvitationsAndNotifications` migration.

**Api**:
- `POST /api/boards/{id}/invitations` (Owner-only, replaces the old `POST .../members`).
- `PATCH /api/boards/{id}/members/{userId}/role` (Owner-only).
- `POST /api/invitations/{id}/respond` (new `InvitationsController`).
- `GET /api/notifications`, `POST /api/notifications/{id}/read` (new `NotificationsController`).

**Frontend**:
- `NotificationService` — holds a `notifications` signal so the bell badge and dropdown always
  agree; `refresh()`, `markRead()`, `respondToInvitation()`.
- `NotificationBellComponent` — lives in the app shell (`app.component`), visible whenever
  `currentUser.isAuthenticated()`. Polls every 20s plus an immediate fetch on init. Pending
  `BoardInvitation` notifications get Accept/Decline buttons; everything else just marks read
  on click.
- `board-detail`: the "add member" picker (select an existing user + role) is replaced by a
  plain email input ("invite"); the members list shows a role `<select>` for the Owner instead
  of a static label (in-place promote/demote) and keeps the remove button; the assignee
  dropdown and "Assign to me" button only render when `isOwner()` — Members see a read-only
  colored assignee label.

## Verification

- `dotnet test`: 102 unit (new: `BoardInvitationTests`, `NotificationTests`,
  `InviteBoardMemberCommandHandlerTests`, `RespondToBoardInvitationCommandHandlerTests`,
  `UpdateBoardMemberRoleCommandHandlerTests`, `GetNotificationsQueryHandlerTests`,
  `MarkNotificationReadCommandHandlerTests`, `TransitionTaskStateCommandHandlerTests`,
  plus new cases in `AssignTaskCommandHandlerTests`/`CreateUserCommandHandlerTests`) + 15
  integration (rewrote `BoardMembersEndpointsTests` for the invite/accept flow, added
  `NotificationsEndpointsTests` covering the not-yet-registered-invitee path, decline,
  assignment notifications, and mark-read), all passing.
- `ng test`: 79, all passing. `ng lint`: clean. `ng build --configuration production`: succeeds.
- Rebuilt the `api`/`frontend` containers and walked the full flow live: invite → notification →
  accept → Member gets 403 trying to self-assign → Owner assigns → Member gets a
  `TaskAssigned` notification → Member moves their own task's state successfully.
