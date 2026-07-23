using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Notifications.Commands.MarkNotificationRead;

public sealed class MarkNotificationReadCommandHandler(ITaskFlowDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<MarkNotificationReadCommand>
{
    public async Task Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await context.Notifications
            .FirstOrDefaultAsync(
                n => n.Id == request.NotificationId && n.RecipientUserId == currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Notification), request.NotificationId);

        notification.MarkAsRead();
        await context.SaveChangesAsync(cancellationToken);
    }
}
