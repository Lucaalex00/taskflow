using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Boards.Commands.RenameBoard;

public sealed class RenameBoardCommandHandler(ITaskFlowDbContext context, IBoardAuthorizer boardAuthorizer)
    : IRequestHandler<RenameBoardCommand>
{
    public async Task Handle(RenameBoardCommand request, CancellationToken cancellationToken)
    {
        await boardAuthorizer.EnsureOwnerAsync(request.BoardId, cancellationToken);

        var board = await context.Boards
            .FirstOrDefaultAsync(b => b.Id == request.BoardId, cancellationToken)
            ?? throw new NotFoundException(nameof(ProjectBoard), request.BoardId);

        var result = board.Rename(request.Name);
        if (!result.IsSuccess)
            throw new Common.Exceptions.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(request.Name), result.Error)
            ]);

        await context.SaveChangesAsync(cancellationToken);
    }
}
