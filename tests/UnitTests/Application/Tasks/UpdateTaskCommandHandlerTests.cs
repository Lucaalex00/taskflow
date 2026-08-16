using FluentAssertions;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Tasks.Commands.UpdateTask;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.UnitTests.Common;
using Xunit;

namespace TaskFlow.UnitTests.Application.Tasks;

public class UpdateTaskCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static async Task<(TestDbContext Context, TaskItem Task)> SeedTaskAsync()
    {
        var context = new TestDbContext();
        var board = ProjectBoard.Create("Sprint 1", Guid.NewGuid()).Value;
        var task = TaskItem.Create(board.Id, "Original", "old desc", TaskPriority.Low, null).Value;
        context.Boards.Add(board);
        context.Tasks.Add(task);
        await context.SaveChangesAsync();
        return (context, task);
    }

    [Fact]
    public async Task Handle_UpdatesTheTaskContent()
    {
        var (context, task) = await SeedTaskAsync();
        var handler = new UpdateTaskCommandHandler(context, new FakeBoardAuthorizer(), new FakeDateTimeProvider(Now));

        await handler.Handle(
            new UpdateTaskCommand(task.Id, "Updated", "new desc", TaskPriority.High, null), CancellationToken.None);

        var saved = await context.Tasks.FindAsync(task.Id);
        saved!.Title.Should().Be("Updated");
        saved.Priority.Should().Be(TaskPriority.High);
    }

    [Fact]
    public async Task Handle_WithAnEmptyTitle_ThrowsValidationException()
    {
        var (context, task) = await SeedTaskAsync();
        var handler = new UpdateTaskCommandHandler(context, new FakeBoardAuthorizer(), new FakeDateTimeProvider(Now));

        var act = async () => await handler.Handle(
            new UpdateTaskCommand(task.Id, "   ", null, TaskPriority.Low, null), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_ForAMissingTask_ThrowsNotFound()
    {
        var context = new TestDbContext();
        var handler = new UpdateTaskCommandHandler(context, new FakeBoardAuthorizer(), new FakeDateTimeProvider(Now));

        var act = async () => await handler.Handle(
            new UpdateTaskCommand(Guid.NewGuid(), "x", null, TaskPriority.Low, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
