using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Tasks.Commands.UpdateTask;

public sealed class UpdateTaskCommandHandler(
    ITaskFlowDbContext context, IBoardAuthorizer boardAuthorizer, IDateTimeProvider clock)
    : IRequestHandler<UpdateTaskCommand>
{
    public async Task Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await context.Tasks
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken)
            ?? throw new NotFoundException(nameof(TaskItem), request.TaskId);

        // Editing task content is a board-management action, so it's Owner-only (like creating).
        await boardAuthorizer.EnsureOwnerAsync(task.BoardId, cancellationToken);

        var result = task.UpdateDetails(request.Title, request.Description, request.Priority, request.DueAtUtc, clock.UtcNow);
        if (!result.IsSuccess)
            throw new Common.Exceptions.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(request.Title), result.Error)
            ]);

        await context.SaveChangesAsync(cancellationToken);
    }
}
