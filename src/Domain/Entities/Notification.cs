using TaskFlow.Domain.Common;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Domain.Entities;

/// <summary>
/// An in-app notification for a single recipient. BoardId/TaskId/InvitationId are optional
/// context references — which ones are set depends on Type (e.g. only a BoardInvitation
/// notification carries an InvitationId, letting the UI offer Accept/Decline).
/// </summary>
public class Notification : Entity
{
    public Guid RecipientUserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Message { get; private set; } = null!;
    public Guid? BoardId { get; private set; }
    public Guid? TaskId { get; private set; }
    public Guid? InvitationId { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Notification() { } // EF Core

    private Notification(
        Guid recipientUserId, NotificationType type, string message,
        Guid? boardId, Guid? taskId, Guid? invitationId)
    {
        RecipientUserId = recipientUserId;
        Type = type;
        Message = message;
        BoardId = boardId;
        TaskId = taskId;
        InvitationId = invitationId;
        IsRead = false;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public static Result<Notification> Create(
        Guid recipientUserId, NotificationType type, string message,
        Guid? boardId = null, Guid? taskId = null, Guid? invitationId = null)
    {
        if (recipientUserId == Guid.Empty)
            return Result.Failure<Notification>("A valid recipient is required.");

        if (string.IsNullOrWhiteSpace(message))
            return Result.Failure<Notification>("Notification message cannot be empty.");

        return Result.Success(new Notification(recipientUserId, type, message.Trim(), boardId, taskId, invitationId));
    }

    public void MarkAsRead() => IsRead = true;
}
