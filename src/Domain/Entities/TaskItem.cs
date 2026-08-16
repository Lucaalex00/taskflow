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

    /// <summary>Set when the task is logically deleted ("archived"): it stays in the database
    /// for history but is hidden from the board unless archived tasks are explicitly shown.</summary>
    public DateTime? ArchivedAtUtc { get; private set; }
    public bool IsArchived => ArchivedAtUtc is not null;

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

    /// <summary>Edits a task's content after creation. Rejects a title that's empty/too long and
    /// a *changed* due date set in the past (an existing past due date is left alone so editing
    /// an unrelated field doesn't force you to fix the date). An archived task can't be edited.</summary>
    public Result UpdateDetails(string title, string? description, TaskPriority priority, DateTime? dueAtUtc, DateTime nowUtc)
    {
        if (IsArchived)
            return Result.Failure("Cannot edit an archived task.");

        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure("Task title cannot be empty.");

        if (title.Length > 200)
            return Result.Failure("Task title cannot exceed 200 characters.");

        if (dueAtUtc is not null && dueAtUtc != DueAtUtc && dueAtUtc < nowUtc.Date)
            return Result.Failure("Due date cannot be in the past.");

        Title = title.Trim();
        Description = description?.Trim();
        Priority = priority;
        DueAtUtc = dueAtUtc;
        UpdatedAtUtc = nowUtc;
        return Result.Success();
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

    /// <summary>Logical delete — keeps the row for history but hides it from the board.
    /// Idempotent; only a completed (Done) task can be archived, since archiving is the
    /// "close and file away" step at the end of a task's life.</summary>
    public Result Archive(DateTime nowUtc)
    {
        if (IsArchived)
            return Result.Success();

        if (State != TaskState.Done)
            return Result.Failure("Only a completed task can be archived.");

        ArchivedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
        return Result.Success();
    }
}
