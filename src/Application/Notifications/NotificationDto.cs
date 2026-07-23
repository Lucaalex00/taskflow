using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Notifications;

/// <summary>
/// InvitationStatus is only meaningful (non-null) for Type == BoardInvitation — it tells the
/// UI whether to still show Accept/Decline (Pending) or just display the outcome.
/// </summary>
public sealed record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Message,
    Guid? BoardId,
    Guid? TaskId,
    Guid? InvitationId,
    InvitationStatus? InvitationStatus,
    bool IsRead,
    DateTime CreatedAtUtc);
