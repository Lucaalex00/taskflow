using Microsoft.AspNetCore.SignalR;

namespace TaskFlow.Infrastructure.Realtime;

/// <summary>
/// Clients join a group per board (group name = board id as string) so alerts
/// are only pushed to whoever is actually looking at that board.
/// </summary>
public class AlertsHub : Hub
{
    public async Task JoinBoard(string boardId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, boardId);
    }

    public async Task LeaveBoard(string boardId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, boardId);
    }
}
