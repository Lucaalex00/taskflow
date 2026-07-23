using FluentAssertions;
using TaskFlow.Application.Boards.Commands.CreateBoard;
using TaskFlow.Domain.Enums;
using TaskFlow.UnitTests.Common;
using Xunit;

namespace TaskFlow.UnitTests.Application.Boards;

public class CreateBoardCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesTheBoardOwnedByTheCurrentUserAndAddsThemAsOwnerMember()
    {
        await using var context = new TestDbContext();
        var userId = Guid.NewGuid();
        var handler = new CreateBoardCommandHandler(context, new FakeCurrentUserService(userId));

        var boardId = await handler.Handle(new CreateBoardCommand("Sprint 1"), CancellationToken.None);

        var board = await context.Boards.FindAsync(boardId);
        board!.OwnerId.Should().Be(userId);

        var membership = context.BoardMembers.Single(m => m.BoardId == boardId);
        membership.UserId.Should().Be(userId);
        membership.Role.Should().Be(BoardRole.Owner);
    }
}
