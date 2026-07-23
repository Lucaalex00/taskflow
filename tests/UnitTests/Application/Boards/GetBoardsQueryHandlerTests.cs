using FluentAssertions;
using TaskFlow.Application.Boards.Queries.GetBoards;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.UnitTests.Common;
using Xunit;

namespace TaskFlow.UnitTests.Application.Boards;

public class GetBoardsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsOnlyBoardsTheCurrentUserIsAMemberOf()
    {
        await using var context = new TestDbContext();
        var me = User.Create("me@example.com", "Me", "hash").Value;
        var otherOwner = User.Create("other@example.com", "Other Owner", "hash").Value;
        context.Users.AddRange(me, otherOwner);

        var myBoard = ProjectBoard.Create("My Board", me.Id).Value;
        var otherBoard = ProjectBoard.Create("Someone Else's Board", otherOwner.Id).Value;
        context.Boards.AddRange(myBoard, otherBoard);
        context.BoardMembers.Add(BoardMember.Create(myBoard.Id, me.Id, BoardRole.Owner).Value);
        context.BoardMembers.Add(BoardMember.Create(otherBoard.Id, otherOwner.Id, BoardRole.Owner).Value);
        await context.SaveChangesAsync();

        var handler = new GetBoardsQueryHandler(context, new FakeCurrentUserService(me.Id));

        var boards = await handler.Handle(new GetBoardsQuery(), CancellationToken.None);

        boards.Should().ContainSingle(b => b.Id == myBoard.Id);
        boards.Should().NotContain(b => b.Id == otherBoard.Id);
    }

    [Fact]
    public async Task Handle_IncludesTheOwnersDisplayName()
    {
        await using var context = new TestDbContext();
        var owner = User.Create("owner@example.com", "Ada", "hash").Value;
        context.Users.Add(owner);
        var board = ProjectBoard.Create("Sprint 1", owner.Id).Value;
        context.Boards.Add(board);
        context.BoardMembers.Add(BoardMember.Create(board.Id, owner.Id, BoardRole.Owner).Value);
        await context.SaveChangesAsync();

        var handler = new GetBoardsQueryHandler(context, new FakeCurrentUserService(owner.Id));

        var boards = await handler.Handle(new GetBoardsQuery(), CancellationToken.None);

        boards.Single().OwnerDisplayName.Should().Be("Ada");
    }
}
