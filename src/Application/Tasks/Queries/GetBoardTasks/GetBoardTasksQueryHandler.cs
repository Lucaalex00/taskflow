using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Application.Tasks.Queries.GetBoardTasks;

public sealed class GetBoardTasksQueryHandler(ITaskFlowDbContext context, IDateTimeProvider clock, IBoardAuthorizer boardAuthorizer)
    : IRequestHandler<GetBoardTasksQuery, IReadOnlyList<TaskDto>>
{
    public async Task<IReadOnlyList<TaskDto>> Handle(
        GetBoardTasksQuery request, CancellationToken cancellationToken)
    {
        await boardAuthorizer.EnsureMemberAsync(request.BoardId, cancellationToken);

        var tasks = await context.Tasks
            .AsNoTracking()
            .Where(t => t.BoardId == request.BoardId)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.DueAtUtc)
            .ToListAsync(cancellationToken);

        var now = clock.UtcNow;

        return tasks.Select(t => new TaskDto(
            t.Id, t.BoardId, t.Title, t.Description, t.State, t.Priority,
            t.AssigneeId, t.DueAtUtc, t.IsOverdue(now), t.CreatedAtUtc, t.UpdatedAtUtc)
        ).ToList();
    }
}
