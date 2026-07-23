using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Boards.Commands.UpdateBoardMemberRole;

public sealed class UpdateBoardMemberRoleCommandHandler(ITaskFlowDbContext context, IBoardAuthorizer boardAuthorizer)
    : IRequestHandler<UpdateBoardMemberRoleCommand>
{
    public async Task Handle(UpdateBoardMemberRoleCommand request, CancellationToken cancellationToken)
    {
        await boardAuthorizer.EnsureOwnerAsync(request.BoardId, cancellationToken);

        var membership = await context.BoardMembers
            .FirstOrDefaultAsync(m => m.BoardId == request.BoardId && m.UserId == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(BoardMember), request.UserId);

        if (membership.Role == BoardRole.Owner && request.NewRole != BoardRole.Owner)
        {
            var ownerCount = await context.BoardMembers
                .CountAsync(m => m.BoardId == request.BoardId && m.Role == BoardRole.Owner, cancellationToken);

            if (ownerCount <= 1)
                throw new Common.Exceptions.ValidationException(
                [
                    new FluentValidation.Results.ValidationFailure(nameof(request.NewRole), "A board must keep at least one owner.")
                ]);
        }

        membership.ChangeRole(request.NewRole);
        await context.SaveChangesAsync(cancellationToken);
    }
}
