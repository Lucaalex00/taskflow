using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Users.Commands.CreateUser;

public sealed class CreateUserCommandHandler(
    ITaskFlowDbContext context, IPasswordHasher passwordHasher, IJwtTokenGenerator tokenGenerator)
    : IRequestHandler<CreateUserCommand, AuthResult>
{
    public async Task<AuthResult> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var emailTaken = await context.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (emailTaken)
            throw new Common.Exceptions.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(request.Email), "This email is already registered.")
            ]);

        var result = User.Create(request.Email, request.DisplayName, passwordHasher.Hash(request.Password));

        if (!result.IsSuccess)
            throw new Common.Exceptions.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(request.Email), result.Error)
            ]);

        var user = result.Value;
        context.Users.Add(user);

        // Link any invitations sent to this email before the person had an account, and
        // surface them as notifications now that there's someone to notify.
        var pendingInvitations = await context.BoardInvitations
            .Where(i => i.InviteeEmail == normalizedEmail
                && i.InviteeUserId == null
                && i.Status == InvitationStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var invitation in pendingInvitations)
        {
            invitation.LinkInvitee(user.Id);

            var board = await context.Boards.FirstOrDefaultAsync(b => b.Id == invitation.BoardId, cancellationToken);
            var notificationResult = Notification.Create(
                user.Id, NotificationType.BoardInvitation,
                $"You've been invited to join the board \"{board?.Name ?? "a board"}\".",
                boardId: invitation.BoardId, invitationId: invitation.Id);

            context.Notifications.Add(notificationResult.Value);
        }

        await context.SaveChangesAsync(cancellationToken);

        return new AuthResult(user.Id, user.DisplayName, user.Color, tokenGenerator.GenerateToken(user));
    }
}
