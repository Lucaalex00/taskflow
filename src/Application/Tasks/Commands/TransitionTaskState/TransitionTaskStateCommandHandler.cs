using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Tasks.Commands.TransitionTaskState;

public sealed class TransitionTaskStateCommandHandler(ITaskFlowDbContext context)
    : IRequestHandler<TransitionTaskStateCommand>
{
    public async Task Handle(TransitionTaskStateCommand request, CancellationToken cancellationToken)
    {
        var task = await context.Tasks
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken)
            ?? throw new NotFoundException(nameof(TaskItem), request.TaskId);

        var result = task.TransitionTo(request.NewState);

        if (!result.IsSuccess)
            throw new Common.Exceptions.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(request.NewState), result.Error)
            ]);

        await context.SaveChangesAsync(cancellationToken);
    }
}
