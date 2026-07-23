using FluentAssertions;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Tasks.Commands.TransitionTaskState;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.UnitTests.Common;
using Xunit;

namespace TaskFlow.UnitTests.Application.Tasks;

public class TransitionTaskStateCommandHandlerTests
{
    private static async Task<(TestDbContext Context, TaskItem Task)> SeedAssignedTaskAsync(
        Guid boardId, Guid? assigneeId)
    {
        var context = new TestDbContext();
        var board = ProjectBoard.Create("Sprint 1", Guid.NewGuid()).Value;
        var task = TaskItem.Create(board.Id, "Ship it", null, TaskPriority.Medium, null).Value;
        if (assigneeId.HasValue)
            task.AssignTo(assigneeId.Value);
        context.Boards.Add(board);
        context.Tasks.Add(task);
        await context.SaveChangesAsync();
        return (context, task);
    }

    [Fact]
    public async Task Handle_WithAValidTransition_UpdatesTheTaskState()
    {
        var (context, task) = await SeedAssignedTaskAsync(Guid.NewGuid(), null);
        var handler = new TransitionTaskStateCommandHandler(
            context, new FakeBoardAuthorizer(), new FakeCurrentUserService(Guid.NewGuid()));

        await handler.Handle(new TransitionTaskStateCommand(task.Id, TaskState.InProgress), CancellationToken.None);

        (await context.Tasks.FindAsync(task.Id))!.State.Should().Be(TaskState.InProgress);
    }

    [Fact]
    public async Task Handle_WithAnInvalidTransition_ThrowsValidationException()
    {
        var (context, task) = await SeedAssignedTaskAsync(Guid.NewGuid(), null);
        var handler = new TransitionTaskStateCommandHandler(
            context, new FakeBoardAuthorizer(), new FakeCurrentUserService(Guid.NewGuid()));

        var act = async () => await handler.Handle(
            new TransitionTaskStateCommand(task.Id, TaskState.Done), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_WhenSomeoneElseMovesTheAssigneesTask_NotifiesTheAssignee()
    {
        var assigneeId = Guid.NewGuid();
        var (context, task) = await SeedAssignedTaskAsync(Guid.NewGuid(), assigneeId);
        var handler = new TransitionTaskStateCommandHandler(
            context, new FakeBoardAuthorizer(), new FakeCurrentUserService(Guid.NewGuid()));

        await handler.Handle(new TransitionTaskStateCommand(task.Id, TaskState.InProgress), CancellationToken.None);

        context.Notifications.Should().ContainSingle(
            n => n.RecipientUserId == assigneeId && n.Type == NotificationType.TaskStateChanged);
    }

    [Fact]
    public async Task Handle_WhenTheAssigneeMovesTheirOwnTask_DoesNotNotifyThemselves()
    {
        var assigneeId = Guid.NewGuid();
        var (context, task) = await SeedAssignedTaskAsync(Guid.NewGuid(), assigneeId);
        var handler = new TransitionTaskStateCommandHandler(
            context, new FakeBoardAuthorizer(), new FakeCurrentUserService(assigneeId));

        await handler.Handle(new TransitionTaskStateCommand(task.Id, TaskState.InProgress), CancellationToken.None);

        context.Notifications.Should().BeEmpty();
    }
}
