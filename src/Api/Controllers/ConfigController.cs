using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Configuration.Queries.GetPublicConfig;

namespace TaskFlow.Api.Controllers;

/// <summary>Read-only, anonymous view of how this instance is configured — the login screen
/// calls it before anyone is signed in, to decide whether to offer the demo account.</summary>
[ApiController]
[Route("api/config")]
[AllowAnonymous]
public sealed class ConfigController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PublicConfigDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetPublicConfigQuery(), cancellationToken));
}
