using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Tasks.Commands.CreateTask;

public sealed class CreateTaskCommandHandler(ITaskFlowDbContext context, IBoardAuthorizer boardAuthorizer)
    : IRequestHandler<CreateTaskCommand, Guid>
{
    public async Task<Guid> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var boardExists = await context.Boards
            .AnyAsync(b => b.Id == request.BoardId, cancellationToken);

        if (!boardExists)
            throw new NotFoundException(nameof(ProjectBoard), request.BoardId);

        await boardAuthorizer.EnsureMemberAsync(request.BoardId, cancellationToken);

        var result = TaskItem.Create(
            request.BoardId, request.Title, request.Description, request.Priority, request.DueAtUtc);

        if (!result.IsSuccess)
            throw new Common.Exceptions.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(request.Title), result.Error)
            ]);

        context.Tasks.Add(result.Value);
        await context.SaveChangesAsync(cancellationToken);

        return result.Value.Id;
    }
}
