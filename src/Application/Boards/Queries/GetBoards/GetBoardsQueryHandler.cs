using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Application.Boards.Queries.GetBoards;

public sealed class GetBoardsQueryHandler(ITaskFlowDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<GetBoardsQuery, IReadOnlyList<BoardDto>>
{
    public async Task<IReadOnlyList<BoardDto>> Handle(GetBoardsQuery request, CancellationToken cancellationToken)
    {
        var memberBoardIds = context.BoardMembers
            .Where(m => m.UserId == currentUser.UserId)
            .Select(m => m.BoardId);

        return await context.Boards
            .AsNoTracking()
            .Where(b => memberBoardIds.Contains(b.Id))
            .Join(context.Users, b => b.OwnerId, u => u.Id,
                (b, u) => new BoardDto(b.Id, b.Name, b.OwnerId, u.DisplayName, b.Color, b.Tasks.Count, b.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
