using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Events;

public sealed class TaskCompletedEvent(Guid taskId, Guid boardId, Guid? assigneeId) : IDomainEvent
{
    public Guid TaskId { get; } = taskId;
    public Guid BoardId { get; } = boardId;
    public Guid? AssigneeId { get; } = assigneeId;
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
