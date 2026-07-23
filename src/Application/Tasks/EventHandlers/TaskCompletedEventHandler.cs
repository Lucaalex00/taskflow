using MediatR;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Common.Events;
using TaskFlow.Domain.Events;

namespace TaskFlow.Application.Tasks.EventHandlers;

/// <summary>
/// First real subscriber to TaskCompletedEvent (previously raised but never dispatched —
/// see OVERVIEW.md "Future work"). Logs completion as a structured audit trail; further
/// subscribers (e.g. notifications) can be added without touching TaskItem or this handler.
/// </summary>
public sealed class TaskCompletedEventHandler(ILogger<TaskCompletedEventHandler> logger)
    : INotificationHandler<DomainEventNotification<TaskCompletedEvent>>
{
    public Task Handle(DomainEventNotification<TaskCompletedEvent> notification, CancellationToken cancellationToken)
    {
        var completed = notification.DomainEvent;

        logger.LogInformation(
            "Task {TaskId} on board {BoardId} was completed (assignee: {AssigneeId})",
            completed.TaskId, completed.BoardId, completed.AssigneeId);

        return Task.CompletedTask;
    }
}
