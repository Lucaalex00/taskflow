using FluentAssertions;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Services;
using TaskFlow.Application.Tasks.Commands.CreateTask;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.UnitTests.Common;
using Xunit;

namespace TaskFlow.UnitTests.Application.Tasks;

public class CreateTaskCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithExistingBoard_CreatesTaskAndReturnsItsId()
    {
        await using var context = new TestDbContext();
        var board = ProjectBoard.Create("Sprint 12", Guid.NewGuid()).Value;
        context.Boards.Add(board);
        await context.SaveChangesAsync();

        var handler = new CreateTaskCommandHandler(context, new FakeBoardAuthorizer());
        var command = new CreateTaskCommand(board.Id, "Write ADRs", "Document key decisions", TaskPriority.High, null);

        var taskId = await handler.Handle(command, CancellationToken.None);

        var savedTask = await context.Tasks.FindAsync(taskId);
        savedTask.Should().NotBeNull();
        savedTask!.Title.Should().Be("Write ADRs");
        savedTask.State.Should().Be(TaskState.Todo);
    }

    [Fact]
    public async Task Handle_WithNonExistingBoard_ThrowsNotFoundException()
    {
        await using var context = new TestDbContext();
        var handler = new CreateTaskCommandHandler(context, new FakeBoardAuthorizer());
        var command = new CreateTaskCommand(Guid.NewGuid(), "Orphan task", null, TaskPriority.Low, null);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_AsANonOwnerMember_ThrowsForbiddenException()
    {
        await using var context = new TestDbContext();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var board = ProjectBoard.Create("Sprint 12", ownerId).Value;
        context.Boards.Add(board);
        context.BoardMembers.Add(BoardMember.Create(board.Id, ownerId, BoardRole.Owner).Value);
        context.BoardMembers.Add(BoardMember.Create(board.Id, memberId, BoardRole.Member).Value);
        await context.SaveChangesAsync();

        var handler = new CreateTaskCommandHandler(
            context, new BoardAuthorizer(context, new FakeCurrentUserService(memberId)));
        var command = new CreateTaskCommand(board.Id, "Write ADRs", null, TaskPriority.High, null);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
