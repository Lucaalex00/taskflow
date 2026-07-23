using MediatR;

namespace TaskFlow.Application.Alerts.Queries.GetBoardAlerts;

public sealed record GetBoardAlertsQuery(Guid BoardId, bool UnreadOnly = false) : IRequest<IReadOnlyList<AlertDto>>;
