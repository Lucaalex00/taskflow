using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Boards.Commands.RespondToBoardInvitation;

public sealed class RespondToBoardInvitationCommandHandler(ITaskFlowDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<RespondToBoardInvitationCommand>
{
    public async Task Handle(RespondToBoardInvitationCommand request, CancellationToken cancellationToken)
    {
        var invitation = await context.BoardInvitations
            .FirstOrDefaultAsync(i => i.Id == request.InvitationId, cancellationToken)
            ?? throw new NotFoundException(nameof(BoardInvitation), request.InvitationId);

        if (invitation.InviteeUserId != currentUser.UserId)
            throw new ForbiddenException("This invitation isn't addressed to you.");

        var result = request.Accept ? invitation.Accept() : invitation.Decline();
        if (!result.IsSuccess)
            throw new Common.Exceptions.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(request.Accept), result.Error)
            ]);

        if (request.Accept)
        {
            var membership = BoardMember.Create(invitation.BoardId, currentUser.UserId, BoardRole.Member);
            context.BoardMembers.Add(membership.Value);
        }

        var relatedNotifications = await context.Notifications
            .Where(n => n.InvitationId == invitation.Id && n.RecipientUserId == currentUser.UserId)
            .ToListAsync(cancellationToken);
        foreach (var notification in relatedNotifications)
            notification.MarkAsRead();

        await context.SaveChangesAsync(cancellationToken);
    }
}
