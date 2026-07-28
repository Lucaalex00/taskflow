using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Tasks.Commands.ArchiveTask;

public sealed class ArchiveTaskCommandHandler(
    ITaskFlowDbContext context, IBoardAuthorizer boardAuthorizer, IDateTimeProvider clock)
    : IRequestHandler<ArchiveTaskCommand>
{
    public async Task Handle(ArchiveTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await context.Tasks
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken)
            ?? throw new NotFoundException(nameof(TaskItem), request.TaskId);

        // Archiving (logical delete) is a board-management action, so it's Owner-only.
        await boardAuthorizer.EnsureOwnerAsync(task.BoardId, cancellationToken);

        var result = task.Archive(clock.UtcNow);
        if (!result.IsSuccess)
            throw new Common.Exceptions.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(request.TaskId), result.Error)
            ]);

        await context.SaveChangesAsync(cancellationToken);
    }
}
