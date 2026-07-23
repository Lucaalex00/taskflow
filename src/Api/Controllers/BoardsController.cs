using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Boards;
using TaskFlow.Application.Boards.Commands.CreateBoard;
using TaskFlow.Application.Boards.Commands.InviteBoardMember;
using TaskFlow.Application.Boards.Commands.RemoveBoardMember;
using TaskFlow.Application.Boards.Commands.UpdateBoardMemberRole;
using TaskFlow.Application.Boards.Queries.GetBoardMembers;
using TaskFlow.Application.Boards.Queries.GetBoards;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/boards")]
[Authorize]
public sealed class BoardsController(ISender sender) : ControllerBase
{
    /// <summary>Lists every board the current user is a member of, with its current task count.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BoardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var boards = await sender.Send(new GetBoardsQuery(), cancellationToken);
        return Ok(boards);
    }

    /// <summary>Creates a new board (workspace), owned by the current user.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateBoardCommand command, CancellationToken cancellationToken)
    {
        var boardId = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { id = boardId }, boardId);
    }

    /// <summary>Lists a board's members and their roles. Requires board membership.</summary>
    [HttpGet("{boardId:guid}/members")]
    [ProducesResponseType(typeof(IReadOnlyList<BoardMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMembers(Guid boardId, CancellationToken cancellationToken)
    {
        var members = await sender.Send(new GetBoardMembersQuery(boardId), cancellationToken);
        return Ok(members);
    }

    /// <summary>Invites a user (by email) to the board. They join as a Member once they accept.
    /// Owner only.</summary>
    [HttpPost("{boardId:guid}/invitations")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> InviteMember(
        Guid boardId, InviteBoardMemberRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new InviteBoardMemberCommand(boardId, request.Email), cancellationToken);
        return NoContent();
    }

    /// <summary>Changes an existing member's role. Owner only; a board must keep at least one owner.</summary>
    [HttpPatch("{boardId:guid}/members/{userId:guid}/role")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMemberRole(
        Guid boardId, Guid userId, UpdateBoardMemberRoleRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateBoardMemberRoleCommand(boardId, userId, request.Role), cancellationToken);
        return NoContent();
    }

    /// <summary>Removes a member from the board. Owner only; a board must keep at least one owner.</summary>
    [HttpDelete("{boardId:guid}/members/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMember(Guid boardId, Guid userId, CancellationToken cancellationToken)
    {
        await sender.Send(new RemoveBoardMemberCommand(boardId, userId), cancellationToken);
        return NoContent();
    }
}

public sealed record InviteBoardMemberRequest(string Email);
public sealed record UpdateBoardMemberRoleRequest(BoardRole Role);
