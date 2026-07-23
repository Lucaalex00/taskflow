using FluentAssertions;
using TaskFlow.Application.Boards.Commands.AddBoardMember;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Services;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.UnitTests.Common;
using Xunit;

namespace TaskFlow.UnitTests.Application.Boards;

public class AddBoardMemberCommandHandlerTests
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
    public async Task Handle_AsOwner_AddsTheUserAsAMember()
    {
        var (context, boardId, ownerId) = await SeedBoardAsync();
        var newMember = User.Create("member@example.com", "Member", "hash").Value;
        context.Users.Add(newMember);
        await context.SaveChangesAsync();

        var handler = new AddBoardMemberCommandHandler(
            context, new BoardAuthorizer(context, new FakeCurrentUserService(ownerId)));

        await handler.Handle(new AddBoardMemberCommand(boardId, newMember.Id, BoardRole.Member), CancellationToken.None);

        context.BoardMembers.Should().Contain(m => m.BoardId == boardId && m.UserId == newMember.Id && m.Role == BoardRole.Member);
    }

    [Fact]
    public async Task Handle_AsNonOwner_ThrowsForbiddenException()
    {
        var (context, boardId, _) = await SeedBoardAsync();
        var newMember = User.Create("member@example.com", "Member", "hash").Value;
        context.Users.Add(newMember);
        await context.SaveChangesAsync();

        var handler = new AddBoardMemberCommandHandler(
            context, new BoardAuthorizer(context, new FakeCurrentUserService(Guid.NewGuid())));

        var act = async () => await handler.Handle(
            new AddBoardMemberCommand(boardId, newMember.Id, BoardRole.Member), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_WhenUserIsAlreadyAMember_ThrowsValidationException()
    {
        var (context, boardId, ownerId) = await SeedBoardAsync();

        var handler = new AddBoardMemberCommandHandler(
            context, new BoardAuthorizer(context, new FakeCurrentUserService(ownerId)));

        var act = async () => await handler.Handle(
            new AddBoardMemberCommand(boardId, ownerId, BoardRole.Member), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }
}
