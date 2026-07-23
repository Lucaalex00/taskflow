using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Notifications.Queries.GetNotifications;

public sealed class GetNotificationsQueryHandler(ITaskFlowDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<GetNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    public async Task<IReadOnlyList<NotificationDto>> Handle(
        GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var query =
            from n in context.Notifications.AsNoTracking()
            where n.RecipientUserId == currentUser.UserId
            join inv in context.BoardInvitations on n.InvitationId equals inv.Id into invitationGroup
            from inv in invitationGroup.DefaultIfEmpty()
            orderby n.CreatedAtUtc descending
            select new NotificationDto(
                n.Id, n.Type, n.Message, n.BoardId, n.TaskId, n.InvitationId,
                inv != null ? inv.Status : (InvitationStatus?)null, n.IsRead, n.CreatedAtUtc);

        return await query.ToListAsync(cancellationToken);
    }
}
