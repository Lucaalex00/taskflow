using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Boards.Commands.RespondToBoardInvitation;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/invitations")]
[Authorize]
public sealed class InvitationsController(ISender sender) : ControllerBase
{
    /// <summary>Accepts or declines a board invitation addressed to the current user.</summary>
    [HttpPost("{invitationId:guid}/respond")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Respond(
        Guid invitationId, RespondToInvitationRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new RespondToBoardInvitationCommand(invitationId, request.Accept), cancellationToken);
        return NoContent();
    }
}

public sealed record RespondToInvitationRequest(bool Accept);
