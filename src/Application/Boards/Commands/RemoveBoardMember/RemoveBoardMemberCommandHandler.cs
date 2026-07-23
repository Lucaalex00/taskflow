using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Boards.Commands.RemoveBoardMember;

public sealed class RemoveBoardMemberCommandHandler(ITaskFlowDbContext context, IBoardAuthorizer boardAuthorizer)
    : IRequestHandler<RemoveBoardMemberCommand>
{
    public async Task Handle(RemoveBoardMemberCommand request, CancellationToken cancellationToken)
    {
        await boardAuthorizer.EnsureOwnerAsync(request.BoardId, cancellationToken);

        var membership = await context.BoardMembers
            .FirstOrDefaultAsync(m => m.BoardId == request.BoardId && m.UserId == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(BoardMember), request.UserId);

        if (membership.Role == BoardRole.Owner)
        {
            var ownerCount = await context.BoardMembers
                .CountAsync(m => m.BoardId == request.BoardId && m.Role == BoardRole.Owner, cancellationToken);

            if (ownerCount <= 1)
                throw new Common.Exceptions.ValidationException(
                [
                    new FluentValidation.Results.ValidationFailure(nameof(request.UserId), "A board must keep at least one owner.")
                ]);
        }

        context.BoardMembers.Remove(membership);
        await context.SaveChangesAsync(cancellationToken);
    }
}
