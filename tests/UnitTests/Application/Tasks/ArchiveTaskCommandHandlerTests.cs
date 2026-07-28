using FluentAssertions;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Tasks.Commands.ArchiveTask;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.UnitTests.Common;
using Xunit;

namespace TaskFlow.UnitTests.Application.Tasks;

public class ArchiveTaskCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static async Task<(TestDbContext Context, TaskItem Task)> SeedDoneTaskAsync()
    {
        var context = new TestDbContext();
        var board = ProjectBoard.Create("Sprint 1", Guid.NewGuid()).Value;
        var task = TaskItem.Create(board.Id, "Ship it", null, TaskPriority.Medium, null).Value;
        task.TransitionTo(TaskState.InProgress);
        task.TransitionTo(TaskState.Done);
        context.Boards.Add(board);
        context.Tasks.Add(task);
        await context.SaveChangesAsync();
        return (context, task);
    }

    [Fact]
    public async Task Handle_ArchivesADoneTask()
    {
        var (context, task) = await SeedDoneTaskAsync();
        var handler = new ArchiveTaskCommandHandler(context, new FakeBoardAuthorizer(), new FakeDateTimeProvider(Now));

        await handler.Handle(new ArchiveTaskCommand(task.Id), CancellationToken.None);

        (await context.Tasks.FindAsync(task.Id))!.IsArchived.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ForANonDoneTask_ThrowsValidationException()
    {
        var context = new TestDbContext();
        var board = ProjectBoard.Create("Sprint 1", Guid.NewGuid()).Value;
        var task = TaskItem.Create(board.Id, "Not done", null, TaskPriority.Low, null).Value; // still Todo
        context.Boards.Add(board);
        context.Tasks.Add(task);
        await context.SaveChangesAsync();
        var handler = new ArchiveTaskCommandHandler(context, new FakeBoardAuthorizer(), new FakeDateTimeProvider(Now));

        var act = async () => await handler.Handle(new ArchiveTaskCommand(task.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_ForAMissingTask_ThrowsNotFound()
    {
        var context = new TestDbContext();
        var handler = new ArchiveTaskCommandHandler(context, new FakeBoardAuthorizer(), new FakeDateTimeProvider(Now));

        var act = async () => await handler.Handle(new ArchiveTaskCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
