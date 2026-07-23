using FluentAssertions;
using TaskFlow.Application.Boards.Commands.InviteBoardMember;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Services;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.UnitTests.Common;
using Xunit;

namespace TaskFlow.UnitTests.Application.Boards;

public class InviteBoardMemberCommandHandlerTests
{
    private static async Task<(TestDbContext Context, Guid BoardId, Guid OwnerId)> SeedBoardAsync()
    {
        var context = new TestDbContext();
        var owner = User.Create("owner@example.com", "Owner", "hash").Value;
        var board = ProjectBoard.Create("Sprint 1", owner.Id).Value;
        context.Users.Add(owner);
        context.Boards.Add(board);
        context.BoardMembers.Add(BoardMember.Create(board.Id, owner.Id, BoardRole.Owner).Value);
        await context.SaveChangesAsync();
        return (context, board.Id, owner.Id);
    }

    [Fact]
    public async Task Handle_InvitingARegisteredUser_CreatesAPendingInvitationAndNotifiesThem()
    {
        var (context, boardId, ownerId) = await SeedBoardAsync();
        var invitee = User.Create("invitee@example.com", "Invitee", "hash").Value;
        context.Users.Add(invitee);
        await context.SaveChangesAsync();

        var handler = new InviteBoardMemberCommandHandler(
            context, new BoardAuthorizer(context, new FakeCurrentUserService(ownerId)), new FakeCurrentUserService(ownerId));

        await handler.Handle(new InviteBoardMemberCommand(boardId, "invitee@example.com"), CancellationToken.None);

        var invitation = context.BoardInvitations.Single(i => i.BoardId == boardId);
        invitation.InviteeUserId.Should().Be(invitee.Id);
        invitation.Status.Should().Be(InvitationStatus.Pending);

        context.Notifications.Should().ContainSingle(
            n => n.RecipientUserId == invitee.Id && n.Type == NotificationType.BoardInvitation);
    }

    [Fact]
    public async Task Handle_InvitingAnUnregisteredEmail_CreatesAPendingInvitationWithoutANotification()
    {
        var (context, boardId, ownerId) = await SeedBoardAsync();

        var handler = new InviteBoardMemberCommandHandler(
            context, new BoardAuthorizer(context, new FakeCurrentUserService(ownerId)), new FakeCurrentUserService(ownerId));

        await handler.Handle(new InviteBoardMemberCommand(boardId, "notyet@example.com"), CancellationToken.None);

        var invitation = context.BoardInvitations.Single(i => i.BoardId == boardId);
        invitation.InviteeUserId.Should().BeNull();
        context.Notifications.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_AsNonOwner_ThrowsForbiddenException()
    {
        var (context, boardId, _) = await SeedBoardAsync();

        var handler = new InviteBoardMemberCommandHandler(
            context, new BoardAuthorizer(context, new FakeCurrentUserService(Guid.NewGuid())), new FakeCurrentUserService(Guid.NewGuid()));

        var act = async () => await handler.Handle(
            new InviteBoardMemberCommand(boardId, "someone@example.com"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_WhenTheEmailAlreadyHasAPendingInvitation_ThrowsValidationException()
    {
        var (context, boardId, ownerId) = await SeedBoardAsync();
        var handler = new InviteBoardMemberCommandHandler(
            context, new BoardAuthorizer(context, new FakeCurrentUserService(ownerId)), new FakeCurrentUserService(ownerId));
        await handler.Handle(new InviteBoardMemberCommand(boardId, "notyet@example.com"), CancellationToken.None);

        var act = async () => await handler.Handle(
            new InviteBoardMemberCommand(boardId, "notyet@example.com"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_WhenTheUserIsAlreadyAMember_ThrowsValidationException()
    {
        var (context, boardId, ownerId) = await SeedBoardAsync();
        var handler = new InviteBoardMemberCommandHandler(
            context, new BoardAuthorizer(context, new FakeCurrentUserService(ownerId)), new FakeCurrentUserService(ownerId));

        var act = async () => await handler.Handle(
            new InviteBoardMemberCommand(boardId, "owner@example.com"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }
}
