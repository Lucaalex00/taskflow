using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Tasks.Commands.TransitionTaskState;

public sealed class TransitionTaskStateCommandHandler(ITaskFlowDbContext context, IBoardAuthorizer boardAuthorizer, ICurrentUserService currentUser)
    : IRequestHandler<TransitionTaskStateCommand>
{
    public async Task Handle(TransitionTaskStateCommand request, CancellationToken cancellationToken)
    {
        var task = await context.Tasks
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken)
            ?? throw new NotFoundException(nameof(TaskItem), request.TaskId);

        await boardAuthorizer.EnsureMemberAsync(task.BoardId, cancellationToken);

        var result = task.TransitionTo(request.NewState);

        if (!result.IsSuccess)
            throw new Common.Exceptions.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(request.NewState), result.Error)
            ]);

        // Notify the assignee when someone else moves their task — never for your own actions.
        if (task.AssigneeId.HasValue && task.AssigneeId.Value != currentUser.UserId)
        {
            var notificationResult = Notification.Create(
                task.AssigneeId.Value, NotificationType.TaskStateChanged,
                $"\"{task.Title}\" was moved to {request.NewState}.", boardId: task.BoardId, taskId: task.Id);

            context.Notifications.Add(notificationResult.Value);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
