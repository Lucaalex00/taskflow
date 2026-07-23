using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Boards.Commands.InviteBoardMember;

public sealed class InviteBoardMemberCommandHandler(ITaskFlowDbContext context, IBoardAuthorizer boardAuthorizer, ICurrentUserService currentUser)
    : IRequestHandler<InviteBoardMemberCommand>
{
    public async Task Handle(InviteBoardMemberCommand request, CancellationToken cancellationToken)
    {
        await boardAuthorizer.EnsureOwnerAsync(request.BoardId, cancellationToken);

        var board = await context.Boards.FirstOrDefaultAsync(b => b.Id == request.BoardId, cancellationToken)
            ?? throw new NotFoundException(nameof(ProjectBoard), request.BoardId);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var invitee = await context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (invitee is not null)
        {
            var alreadyMember = await context.BoardMembers
                .AnyAsync(m => m.BoardId == request.BoardId && m.UserId == invitee.Id, cancellationToken);
            if (alreadyMember)
                throw new Common.Exceptions.ValidationException(
                [
                    new FluentValidation.Results.ValidationFailure(nameof(request.Email), "This user is already a member of the board.")
                ]);
        }

        var alreadyInvited = await context.BoardInvitations.AnyAsync(
            i => i.BoardId == request.BoardId && i.InviteeEmail == normalizedEmail && i.Status == InvitationStatus.Pending,
            cancellationToken);
        if (alreadyInvited)
            throw new Common.Exceptions.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(request.Email), "This email already has a pending invitation to this board.")
            ]);

        var invitationResult = BoardInvitation.Create(request.BoardId, normalizedEmail, invitee?.Id, currentUser.UserId);
        if (!invitationResult.IsSuccess)
            throw new Common.Exceptions.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(request.Email), invitationResult.Error)
            ]);

        var invitation = invitationResult.Value;
        context.BoardInvitations.Add(invitation);

        if (invitee is not null)
        {
            var notificationResult = Notification.Create(
                invitee.Id, NotificationType.BoardInvitation,
                $"You've been invited to join the board \"{board.Name}\".",
                boardId: board.Id, invitationId: invitation.Id);

            context.Notifications.Add(notificationResult.Value);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
