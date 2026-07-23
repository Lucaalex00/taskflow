using MediatR;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Boards;
using TaskFlow.Application.Boards.Commands.CreateBoard;
using TaskFlow.Application.Boards.Queries.GetBoards;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/boards")]
public sealed class BoardsController(ISender sender) : ControllerBase
{
    /// <summary>Lists every board with its current task count.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BoardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var boards = await sender.Send(new GetBoardsQuery(), cancellationToken);
        return Ok(boards);
    }

    /// <summary>Creates a new board (workspace) owned by the given user.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(CreateBoardCommand command, CancellationToken cancellationToken)
    {
        var boardId = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { id = boardId }, boardId);
    }
}
