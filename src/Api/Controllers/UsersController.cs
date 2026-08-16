using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TaskFlow.Application.Users;
using TaskFlow.Application.Users.Commands.ChangePassword;
using TaskFlow.Application.Users.Commands.CreateUser;
using TaskFlow.Application.Users.Commands.UpdateProfile;
using TaskFlow.Application.Users.Commands.UpdateUserColor;
using TaskFlow.Application.Users.Queries.GetUsers;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
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

    /// <summary>Registers a new user and logs them in immediately, returning a JWT.</summary>
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAll), null, result);
    }

    /// <summary>Renames the signed-in user (the user is taken from the token).</summary>
    [HttpPatch("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMyProfile(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateProfileCommand(request.DisplayName), cancellationToken);
        return Ok(result);
    }

    /// <summary>Changes the signed-in user's password, verifying the current one first.</summary>
    [HttpPost("me/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangeMyPassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new ChangePasswordCommand(request.CurrentPassword, request.NewPassword), cancellationToken);
        return NoContent();
    }

    /// <summary>Updates the signed-in user's avatar color (the user is taken from the token).</summary>
    [HttpPatch("me/color")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMyColor(UpdateUserColorRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateUserColorCommand(request.Color), cancellationToken);
        return Ok(result);
    }
}

public sealed record UpdateUserColorRequest(string Color);

public sealed record UpdateProfileRequest(string DisplayName);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
