using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Application.Boards.Queries.GetBoardMembers;

public sealed class GetBoardMembersQueryHandler(ITaskFlowDbContext context, IBoardAuthorizer boardAuthorizer)
    : IRequestHandler<GetBoardMembersQuery, IReadOnlyList<BoardMemberDto>>
{
    public async Task<IReadOnlyList<BoardMemberDto>> Handle(
        GetBoardMembersQuery request, CancellationToken cancellationToken)
    {
        await boardAuthorizer.EnsureMemberAsync(request.BoardId, cancellationToken);

        return await context.BoardMembers
            .AsNoTracking()
            .Where(m => m.BoardId == request.BoardId)
            .Join(context.Users, m => m.UserId, u => u.Id,
                (m, u) => new { m.Role, u.Id, u.DisplayName, u.Email, u.Color })
            .OrderBy(x => x.DisplayName)
            .Select(x => new BoardMemberDto(x.Id, x.DisplayName, x.Email, x.Color, x.Role))
            .ToListAsync(cancellationToken);
    }
}
