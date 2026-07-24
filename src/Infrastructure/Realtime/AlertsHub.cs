using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Infrastructure.Realtime;

/// <summary>
/// Clients join a group per board (group name = board id as string) so alerts
/// are only pushed to whoever is actually looking at that board. Requires the same JWT
/// bearer token used for the REST API, and only lets a connection join a board it's
/// actually a member of — otherwise anyone who guessed a board id could listen in.
/// </summary>
[Authorize]
public class AlertsHub(ITaskFlowDbContext context) : Hub
{
    public async Task JoinBoard(string boardId)
    {
        if (!Guid.TryParse(boardId, out var parsedBoardId) || !await IsMemberAsync(parsedBoardId))
        {
            throw new HubException("You are not a member of this board.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, boardId);
    }

    public async Task LeaveBoard(string boardId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, boardId);
    }

    private async Task<bool> IsMemberAsync(Guid boardId)
    {
        var subject = Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (subject is null || !Guid.TryParse(subject, out var userId))
        {
            return false;
        }

        return await context.BoardMembers
            .AnyAsync(m => m.BoardId == boardId && m.UserId == userId);
    }
}
