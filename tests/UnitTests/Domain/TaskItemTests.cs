using FluentAssertions;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using Xunit;

namespace TaskFlow.UnitTests.Domain;

public class TaskItemTests
{
    private static TaskItem CreateValidTask() =>
        TaskItem.Create(Guid.NewGuid(), "Write unit tests", null, TaskPriority.Medium, dueAtUtc: null).Value;

    [Fact]
    public void Create_WithEmptyTitle_ReturnsFailure()
    {
        var result = TaskItem.Create(Guid.NewGuid(), "  ", null, TaskPriority.Low, null);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("title");
    }

    [Fact]
    public void Create_WithPastDueDate_ReturnsFailure()
    {
        var result = TaskItem.Create(
            Guid.NewGuid(), "Late task", null, TaskPriority.Low, DateTime.UtcNow.AddDays(-1));

        result.IsSuccess.Should().BeFalse();
    }

    [Theory]
    [InlineData(TaskState.Todo, TaskState.InProgress, true)]
    [InlineData(TaskState.Todo, TaskState.Done, false)] // must go through InProgress first
    [InlineData(TaskState.InProgress, TaskState.Blocked, true)]
    [InlineData(TaskState.InProgress, TaskState.Done, true)]
    [InlineData(TaskState.Blocked, TaskState.Done, false)] // must return to InProgress first
    [InlineData(TaskState.Done, TaskState.InProgress, false)] // terminal state, no way back
    [InlineData(TaskState.Cancelled, TaskState.Todo, false)] // terminal state, no way back
    public void TransitionTo_EnforcesAllowedStateMachine(TaskState from, TaskState to, bool expectedSuccess)
    {
        var task = CreateValidTask();

        // Drive the task into the "from" state via valid transitions first.
        foreach (var step in PathTo(from))
            task.TransitionTo(step).IsSuccess.Should().BeTrue();

        var result = task.TransitionTo(to);

        result.IsSuccess.Should().Be(expectedSuccess);
    }

    [Fact]
    public void TransitionTo_Done_SetsCompletedAtUtc()
    {
        var task = CreateValidTask();
        task.TransitionTo(TaskState.InProgress);

        task.TransitionTo(TaskState.Done);

        task.CompletedAtUtc.Should().NotBeNull();
        task.CompletedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void IsOverdue_WhenDueDateInPastAndNotDone_ReturnsTrue()
    {
        var task = TaskItem.Create(
            Guid.NewGuid(), "Deadline missed", null, TaskPriority.High, DateTime.UtcNow.AddDays(5)).Value;

        // Simulate "5 days from now" being in the past relative to a later check point.
        var checkPoint = DateTime.UtcNow.AddDays(10);

        task.IsOverdue(checkPoint).Should().BeTrue();
    }

    [Fact]
    public void IsOverdue_WhenTaskIsDone_ReturnsFalseEvenPastDueDate()
    {
        var task = TaskItem.Create(
            Guid.NewGuid(), "Finished late", null, TaskPriority.High, DateTime.UtcNow.AddDays(1)).Value;
        task.TransitionTo(TaskState.InProgress);
        task.TransitionTo(TaskState.Done);

        task.IsOverdue(DateTime.UtcNow.AddDays(10)).Should().BeFalse();
    }

    [Fact]
    public void AssignTo_WhenTaskAlreadyDone_ReturnsFailure()
    {
        var task = CreateValidTask();
        task.TransitionTo(TaskState.InProgress);
        task.TransitionTo(TaskState.Done);

        var result = task.AssignTo(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void UpdateDetails_WithValidChanges_UpdatesTheTask()
    {
        var task = CreateValidTask();

        var result = task.UpdateDetails("New title", "New desc", TaskPriority.High, dueAtUtc: null, DateTime.UtcNow);

        result.IsSuccess.Should().BeTrue();
        task.Title.Should().Be("New title");
        task.Description.Should().Be("New desc");
        task.Priority.Should().Be(TaskPriority.High);
    }

    [Fact]
    public void UpdateDetails_WithAnEmptyTitle_ReturnsFailure()
    {
        var task = CreateValidTask();

        var result = task.UpdateDetails("  ", null, TaskPriority.Low, null, DateTime.UtcNow);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void UpdateDetails_SettingAChangedPastDueDate_ReturnsFailure()
    {
        var task = CreateValidTask();

        var result = task.UpdateDetails("Title", null, TaskPriority.Low, DateTime.UtcNow.AddDays(-2), DateTime.UtcNow);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void UpdateDetails_KeepingAnExistingPastDueDate_IsAllowed()
    {
        // A task whose (once-valid) due date has since passed: editing an unrelated field must
        // not force the date to change. Simulated by advancing "now" past the due date.
        var due = DateTime.UtcNow.Date.AddDays(10); // future, so Create accepts it
        var task = TaskItem.Create(Guid.NewGuid(), "Old", null, TaskPriority.Low, due).Value;
        var laterNow = due.AddDays(30); // due is now in the past relative to this

        var result = task.UpdateDetails("Renamed", null, TaskPriority.High, due, laterNow);

        result.IsSuccess.Should().BeTrue();
        task.Title.Should().Be("Renamed");
    }

    [Fact]
    public void UpdateDetails_OnAnArchivedTask_ReturnsFailure()
    {
        var task = CreateValidTask();
        task.TransitionTo(TaskState.InProgress);
        task.TransitionTo(TaskState.Done);
        task.Archive(DateTime.UtcNow);

        var result = task.UpdateDetails("Nope", null, TaskPriority.Low, null, DateTime.UtcNow);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Archive_WhenTaskIsDone_MarksItArchived()
    {
        var task = CreateValidTask();
        task.TransitionTo(TaskState.InProgress);
        task.TransitionTo(TaskState.Done);

        var result = task.Archive(DateTime.UtcNow);

        result.IsSuccess.Should().BeTrue();
        task.IsArchived.Should().BeTrue();
        task.ArchivedAtUtc.Should().NotBeNull();
    }

    [Theory]
    [InlineData(TaskState.Todo)]
    [InlineData(TaskState.InProgress)]
    [InlineData(TaskState.Blocked)]
    public void Archive_WhenTaskIsNotDone_ReturnsFailure(TaskState state)
    {
        var task = CreateValidTask();
        foreach (var s in PathTo(state)) task.TransitionTo(s);

        var result = task.Archive(DateTime.UtcNow);

        result.IsSuccess.Should().BeFalse();
        task.IsArchived.Should().BeFalse();
    }

    [Fact]
    public void Archive_IsIdempotent()
    {
        var task = CreateValidTask();
        task.TransitionTo(TaskState.InProgress);
        task.TransitionTo(TaskState.Done);
        task.Archive(DateTime.UtcNow);
        var firstArchivedAt = task.ArchivedAtUtc;

        var result = task.Archive(DateTime.UtcNow.AddHours(1));

        result.IsSuccess.Should().BeTrue();
        task.ArchivedAtUtc.Should().Be(firstArchivedAt); // unchanged on the second call
    }

    /// <summary>Returns the sequence of transitions needed to reach the given state from Todo.</summary>
    private static IEnumerable<TaskState> PathTo(TaskState target) => target switch
    {
        TaskState.Todo => [],
        TaskState.InProgress => [TaskState.InProgress],
        TaskState.Blocked => [TaskState.InProgress, TaskState.Blocked],
        TaskState.Done => [TaskState.InProgress, TaskState.Done],
        TaskState.Cancelled => [TaskState.Cancelled],
        _ => throw new ArgumentOutOfRangeException(nameof(target))
    };
}
