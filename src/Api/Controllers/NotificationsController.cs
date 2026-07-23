using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Notifications;
using TaskFlow.Application.Notifications.Commands.MarkNotificationRead;
using TaskFlow.Application.Notifications.Queries.GetNotifications;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController(ISender sender) : ControllerBase
{
    /// <summary>Lists the current user's notifications, most recent first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var notifications = await sender.Send(new GetNotificationsQuery(), cancellationToken);
        return Ok(notifications);
    }

    /// <summary>Marks a single notification as read.</summary>
    [HttpPost("{notificationId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(Guid notificationId, CancellationToken cancellationToken)
    {
        await sender.Send(new MarkNotificationReadCommand(notificationId), cancellationToken);
        return NoContent();
    }
}
