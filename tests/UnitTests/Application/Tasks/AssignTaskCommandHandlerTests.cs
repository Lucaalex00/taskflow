using FluentAssertions;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Tasks.Commands.AssignTask;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.UnitTests.Common;
using Xunit;

namespace TaskFlow.UnitTests.Application.Tasks;

public class AssignTaskCommandHandlerTests
{
    private static async Task<(TestDbContext Context, Guid BoardId, TaskItem Task)> SeedBoardWithTaskAsync(
        Guid ownerId)
    {
        var context = new TestDbContext();
        var board = ProjectBoard.Create("Sprint 1", ownerId).Value;
        var task = TaskItem.Create(board.Id, "Ship it", null, TaskPriority.Medium, null).Value;
        context.Boards.Add(board);
        context.Tasks.Add(task);
        context.BoardMembers.Add(BoardMember.Create(board.Id, ownerId, BoardRole.Owner).Value);
        await context.SaveChangesAsync();
        return (context, board.Id, task);
    }

    [Fact]
    public async Task Handle_AssigningToABoardMember_Succeeds()
    {
        var ownerId = Guid.NewGuid();
        var (context, boardId, task) = await SeedBoardWithTaskAsync(ownerId);
        var handler = new AssignTaskCommandHandler(context, new FakeBoardAuthorizer());

        await handler.Handle(new AssignTaskCommand(task.Id, ownerId), CancellationToken.None);

        (await context.Tasks.FindAsync(task.Id))!.AssigneeId.Should().Be(ownerId);
    }

    [Fact]
    public async Task Handle_AssigningToSomeoneNotOnTheBoard_ThrowsValidationException()
    {
        var ownerId = Guid.NewGuid();
        var (context, _, task) = await SeedBoardWithTaskAsync(ownerId);
        var handler = new AssignTaskCommandHandler(context, new FakeBoardAuthorizer());

        var act = async () => await handler.Handle(
            new AssignTaskCommand(task.Id, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }
}
