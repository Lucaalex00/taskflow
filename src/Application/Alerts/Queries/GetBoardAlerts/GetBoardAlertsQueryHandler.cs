using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Application.Alerts.Queries.GetBoardAlerts;

public sealed class GetBoardAlertsQueryHandler(ITaskFlowDbContext context)
    : IRequestHandler<GetBoardAlertsQuery, IReadOnlyList<AlertDto>>
{
    public async Task<IReadOnlyList<AlertDto>> Handle(
        GetBoardAlertsQuery request, CancellationToken cancellationToken)
    {
        var query = context.Alerts.AsNoTracking().Where(a => a.BoardId == request.BoardId);

        if (request.UnreadOnly)
            query = query.Where(a => !a.IsRead);

        return await query
            .OrderByDescending(a => a.CreatedAtUtc)
            .Select(a => new AlertDto(
                a.Id, a.BoardId, a.Severity, a.Message, a.RelatedUserId, a.IsRead, a.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
