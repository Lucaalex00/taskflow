using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Tasks;

public sealed record TaskDto(
    Guid Id,
    Guid BoardId,
    string Title,
    string? Description,
    TaskState State,
    TaskPriority Priority,
    Guid? AssigneeId,
    DateTime? DueAtUtc,
    bool IsOverdue,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
