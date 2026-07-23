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
        var userId = Guid.NewGuid();

        var myBoard = ProjectBoard.Create("My Board", userId).Value;
        var otherBoard = ProjectBoard.Create("Someone Else's Board", Guid.NewGuid()).Value;
        context.Boards.AddRange(myBoard, otherBoard);
        context.BoardMembers.Add(BoardMember.Create(myBoard.Id, userId, BoardRole.Owner).Value);
        context.BoardMembers.Add(BoardMember.Create(otherBoard.Id, otherBoard.OwnerId, BoardRole.Owner).Value);
        await context.SaveChangesAsync();

        var handler = new GetBoardsQueryHandler(context, new FakeCurrentUserService(userId));

        var boards = await handler.Handle(new GetBoardsQuery(), CancellationToken.None);

        boards.Should().ContainSingle(b => b.Id == myBoard.Id);
        boards.Should().NotContain(b => b.Id == otherBoard.Id);
    }
}
