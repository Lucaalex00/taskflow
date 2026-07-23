using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Tasks.Commands.AssignTask;

public sealed class AssignTaskCommandHandler(ITaskFlowDbContext context, IBoardAuthorizer boardAuthorizer)
    : IRequestHandler<AssignTaskCommand>
{
    public async Task Handle(AssignTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await context.Tasks
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken)
            ?? throw new NotFoundException(nameof(TaskItem), request.TaskId);

        await boardAuthorizer.EnsureMemberAsync(task.BoardId, cancellationToken);

        var assigneeIsBoardMember = await context.BoardMembers
            .AnyAsync(m => m.BoardId == task.BoardId && m.UserId == request.UserId, cancellationToken);
        if (!assigneeIsBoardMember)
            throw new Common.Exceptions.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(request.UserId), "This user is not a member of the task's board.")
            ]);

        var result = task.AssignTo(request.UserId);

        if (!result.IsSuccess)
            throw new Common.Exceptions.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(request.UserId), result.Error)
            ]);

        await context.SaveChangesAsync(cancellationToken);
    }
}
