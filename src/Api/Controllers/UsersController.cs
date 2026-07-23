using MediatR;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Users;
using TaskFlow.Application.Users.Commands.CreateUser;
using TaskFlow.Application.Users.Queries.GetUsers;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController(ISender sender) : ControllerBase
{
    /// <summary>Lists every registered user, so the UI can offer "assign to..." choices.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var users = await sender.Send(new GetUsersQuery(), cancellationToken);
        return Ok(users);
    }

    /// <summary>Registers a new user.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var userId = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Create), new { id = userId }, userId);
    }
}
