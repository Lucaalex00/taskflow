using TaskFlow.Domain.Common;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Domain.Entities;

/// <summary>
/// An invitation to join a board, addressed by email. If the email doesn't belong to a
/// registered user yet, <see cref="InviteeUserId"/> stays null until that person registers
/// (see CreateUserCommandHandler, which links pending invitations at registration time).
/// Accepting one is what creates the actual <see cref="BoardMember"/> — always as a Member,
/// regardless of who invited; promoting to Owner is a separate, later action.
/// </summary>
public class BoardInvitation : Entity
{
    public Guid BoardId { get; private set; }
    public string InviteeEmail { get; private set; } = null!;
    public Guid? InviteeUserId { get; private set; }
    public Guid InvitedByUserId { get; private set; }
    public InvitationStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? RespondedAtUtc { get; private set; }

    private BoardInvitation() { } // EF Core

    private BoardInvitation(Guid boardId, string inviteeEmail, Guid? inviteeUserId, Guid invitedByUserId)
    {
        BoardId = boardId;
        InviteeEmail = inviteeEmail;
        InviteeUserId = inviteeUserId;
        InvitedByUserId = invitedByUserId;
        Status = InvitationStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public static Result<BoardInvitation> Create(
        Guid boardId, string inviteeEmail, Guid? inviteeUserId, Guid invitedByUserId)
    {
        if (boardId == Guid.Empty)
            return Result.Failure<BoardInvitation>("A valid board is required.");

        if (string.IsNullOrWhiteSpace(inviteeEmail) || !inviteeEmail.Contains('@'))
            return Result.Failure<BoardInvitation>("A valid email address is required.");

        return Result.Success(
            new BoardInvitation(boardId, inviteeEmail.Trim().ToLowerInvariant(), inviteeUserId, invitedByUserId));
    }

    /// <summary>Links a since-registered user to a previously email-only invitation.</summary>
    public void LinkInvitee(Guid userId)
    {
        InviteeUserId = userId;
    }

    public Result Accept()
    {
        if (Status != InvitationStatus.Pending)
            return Result.Failure("This invitation has already been responded to.");

        Status = InvitationStatus.Accepted;
        RespondedAtUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result Decline()
    {
        if (Status != InvitationStatus.Pending)
            return Result.Failure("This invitation has already been responded to.");

        Status = InvitationStatus.Declined;
        RespondedAtUtc = DateTime.UtcNow;
        return Result.Success();
    }
}
