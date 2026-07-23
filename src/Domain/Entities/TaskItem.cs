using TaskFlow.Domain.Common;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Events;

namespace TaskFlow.Domain.Entities;

public class TaskItem : Entity
{
    public Guid BoardId { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public TaskState State { get; private set; }
    public TaskPriority Priority { get; private set; }
    public Guid? AssigneeId { get; private set; }
    public DateTime? DueAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    private TaskItem() { } // EF Core

    private TaskItem(Guid boardId, string title, string? description, TaskPriority priority, DateTime? dueAtUtc)
    {
        BoardId = boardId;
        Title = title;
        Description = description;
        Priority = priority;
        DueAtUtc = dueAtUtc;
        State = TaskState.Todo;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public static Result<TaskItem> Create(
        Guid boardId, string title, string? description, TaskPriority priority, DateTime? dueAtUtc)
    {
        if (boardId == Guid.Empty)
            return Result.Failure<TaskItem>("A task must belong to a board.");

        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure<TaskItem>("Task title cannot be empty.");

        if (title.Length > 200)
            return Result.Failure<TaskItem>("Task title cannot exceed 200 characters.");

        if (dueAtUtc is not null && dueAtUtc < DateTime.UtcNow.Date)
            return Result.Failure<TaskItem>("Due date cannot be in the past.");

        return Result.Success(new TaskItem(boardId, title.Trim(), description?.Trim(), priority, dueAtUtc));
    }

    public Result AssignTo(Guid userId)
    {
        if (userId == Guid.Empty)
            return Result.Failure("A valid user id is required to assign a task.");

        if (State is TaskState.Done or TaskState.Cancelled)
            return Result.Failure($"Cannot assign a task that is already {State}.");

        AssigneeId = userId;
        UpdatedAtUtc = DateTime.UtcNow;
        return Result.Success();
    }

    public Result TransitionTo(TaskState newState)
    {
        if (!IsValidTransition(State, newState))
            return Result.Failure($"Cannot move task from {State} to {newState}.");

        State = newState;
        UpdatedAtUtc = DateTime.UtcNow;

        if (newState == TaskState.Done)
        {
            CompletedAtUtc = UpdatedAtUtc;
            Raise(new TaskCompletedEvent(Id, BoardId, AssigneeId));
        }

        return Result.Success();
    }

    /// <summary>
    /// Explicit state machine — every allowed transition is enumerated so business
    /// rules stay auditable in one place instead of scattered "if" checks.
    /// </summary>
    private static bool IsValidTransition(TaskState from, TaskState to)
    {
        if (from == to) return false;

        return (from, to) switch
        {
            (TaskState.Todo, TaskState.InProgress) => true,
            (TaskState.Todo, TaskState.Cancelled) => true,
            (TaskState.InProgress, TaskState.Blocked) => true,
            (TaskState.InProgress, TaskState.Done) => true,
            (TaskState.InProgress, TaskState.Todo) => true,
            (TaskState.InProgress, TaskState.Cancelled) => true,
            (TaskState.Blocked, TaskState.InProgress) => true,
            (TaskState.Blocked, TaskState.Cancelled) => true,
            _ => false
        };
    }

    public bool IsOverdue(DateTime asOfUtc) =>
        DueAtUtc is not null
        && DueAtUtc < asOfUtc
        && State is not (TaskState.Done or TaskState.Cancelled);
}
