using FluentAssertions;
using TaskFlow.Application.Boards.Commands.RespondToBoardInvitation;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.UnitTests.Common;
using Xunit;

namespace TaskFlow.UnitTests.Application.Boards;

public class RespondToBoardInvitationCommandHandlerTests
{
    private static async Task<(TestDbContext Context, BoardInvitation Invitation, Guid InviteeId)> SeedInvitationAsync()
    {
        var context = new TestDbContext();
        var ownerId = Guid.NewGuid();
        var inviteeId = Guid.NewGuid();
        var board = ProjectBoard.Create("Sprint 1", ownerId).Value;
        var invitation = BoardInvitation.Create(board.Id, "invitee@example.com", inviteeId, ownerId).Value;
        context.Boards.Add(board);
        context.BoardInvitations.Add(invitation);
        var notification = Notification.Create(
            inviteeId, NotificationType.BoardInvitation, "invited", invitationId: invitation.Id).Value;
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();
        return (context, invitation, inviteeId);
    }

    [Fact]
    public async Task Handle_Accepting_CreatesBoardMembershipAsMemberAndMarksNotificationRead()
    {
        var (context, invitation, inviteeId) = await SeedInvitationAsync();
        var handler = new RespondToBoardInvitationCommandHandler(context, new FakeCurrentUserService(inviteeId));

        await handler.Handle(new RespondToBoardInvitationCommand(invitation.Id, Accept: true), CancellationToken.None);

        (await context.BoardInvitations.FindAsync(invitation.Id))!.Status.Should().Be(InvitationStatus.Accepted);
        context.BoardMembers.Should().ContainSingle(
            m => m.BoardId == invitation.BoardId && m.UserId == inviteeId && m.Role == BoardRole.Member);
        context.Notifications.Single().IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Declining_DoesNotCreateMembership()
    {
        var (context, invitation, inviteeId) = await SeedInvitationAsync();
        var handler = new RespondToBoardInvitationCommandHandler(context, new FakeCurrentUserService(inviteeId));

        await handler.Handle(new RespondToBoardInvitationCommand(invitation.Id, Accept: false), CancellationToken.None);

        (await context.BoardInvitations.FindAsync(invitation.Id))!.Status.Should().Be(InvitationStatus.Declined);
        context.BoardMembers.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenTheCurrentUserIsNotTheInvitee_ThrowsForbiddenException()
    {
        var (context, invitation, _) = await SeedInvitationAsync();
        var handler = new RespondToBoardInvitationCommandHandler(context, new FakeCurrentUserService(Guid.NewGuid()));

        var act = async () => await handler.Handle(
            new RespondToBoardInvitationCommand(invitation.Id, Accept: true), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_WhenAlreadyResponded_ThrowsValidationException()
    {
        var (context, invitation, inviteeId) = await SeedInvitationAsync();
        var handler = new RespondToBoardInvitationCommandHandler(context, new FakeCurrentUserService(inviteeId));
        await handler.Handle(new RespondToBoardInvitationCommand(invitation.Id, Accept: true), CancellationToken.None);

        var act = async () => await handler.Handle(
            new RespondToBoardInvitationCommand(invitation.Id, Accept: false), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }
}
