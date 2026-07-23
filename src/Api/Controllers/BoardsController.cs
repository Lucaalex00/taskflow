using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Boards;
using TaskFlow.Application.Boards.Commands.AddBoardMember;
using TaskFlow.Application.Boards.Commands.CreateBoard;
using TaskFlow.Application.Boards.Commands.RemoveBoardMember;
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

    /// <summary>Adds a registered user to the board. Owner only.</summary>
    [HttpPost("{boardId:guid}/members")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddMember(
        Guid boardId, AddBoardMemberRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new AddBoardMemberCommand(boardId, request.UserId, request.Role), cancellationToken);
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

public sealed record AddBoardMemberRequest(Guid UserId, BoardRole Role);
