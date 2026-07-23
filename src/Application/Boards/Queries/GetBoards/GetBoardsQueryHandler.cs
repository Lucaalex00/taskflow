using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Application.Boards.Queries.GetBoards;

public sealed class GetBoardsQueryHandler(ITaskFlowDbContext context)
    : IRequestHandler<GetBoardsQuery, IReadOnlyList<BoardDto>>
{
    public async Task<IReadOnlyList<BoardDto>> Handle(GetBoardsQuery request, CancellationToken cancellationToken)
    {
        return await context.Boards
            .AsNoTracking()
            .Select(b => new BoardDto(b.Id, b.Name, b.OwnerId, b.Tasks.Count, b.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
