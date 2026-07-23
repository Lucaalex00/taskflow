using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Common.Services;

public sealed class BoardAuthorizer(ITaskFlowDbContext context, ICurrentUserService currentUser) : IBoardAuthorizer
{
    public async Task EnsureMemberAsync(Guid boardId, CancellationToken cancellationToken)
    {
        var isMember = await context.BoardMembers
            .AnyAsync(m => m.BoardId == boardId && m.UserId == currentUser.UserId, cancellationToken);

        if (!isMember)
            throw new ForbiddenException("You are not a member of this board.");
    }

    public async Task EnsureOwnerAsync(Guid boardId, CancellationToken cancellationToken)
    {
        var isOwner = await context.BoardMembers
            .AnyAsync(
                m => m.BoardId == boardId && m.UserId == currentUser.UserId && m.Role == BoardRole.Owner,
                cancellationToken);

        if (!isOwner)
            throw new ForbiddenException("Only the board owner can do this.");
    }
}
