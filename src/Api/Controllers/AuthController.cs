using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Users;
using TaskFlow.Application.Users.Commands.Login;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController(ISender sender) : ControllerBase
{
    /// <summary>Logs in with an email/password, returning a JWT for subsequent requests.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LoginCommand(request.Email, request.Password), cancellationToken);
        return Ok(result);
    }
}

public sealed record LoginRequest(string Email, string Password);
