using Microsoft.AspNetCore.SignalR;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Realtime;

public sealed class SignalRAlertNotifier(IHubContext<AlertsHub> hubContext) : IAlertNotifier
{
    public async Task NotifyBoardAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            alert.Id,
            alert.BoardId,
            Severity = alert.Severity.ToString(),
            alert.Message,
            alert.RelatedUserId,
            alert.CreatedAtUtc
        };

        // Group name matches the board id clients join via AlertsHub.JoinBoard.
        await hubContext.Clients
            .Group(alert.BoardId.ToString())
            .SendAsync("AlertRaised", payload, cancellationToken);
    }
}
