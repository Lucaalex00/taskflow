using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Boards.Commands.ArchiveBoard;

public sealed class ArchiveBoardCommandHandler(
    ITaskFlowDbContext context, IBoardAuthorizer boardAuthorizer, IDateTimeProvider clock)
    : IRequestHandler<ArchiveBoardCommand>
{
    public async Task Handle(ArchiveBoardCommand request, CancellationToken cancellationToken)
    {
        await boardAuthorizer.EnsureOwnerAsync(request.BoardId, cancellationToken);

        var board = await context.Boards
            .FirstOrDefaultAsync(b => b.Id == request.BoardId, cancellationToken)
            ?? throw new NotFoundException(nameof(ProjectBoard), request.BoardId);

        board.Archive(clock.UtcNow);
        await context.SaveChangesAsync(cancellationToken);
    }
}
