using MediatR;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.AlertRules.Commands.CreateAlertRule;
using TaskFlow.Application.Alerts;
using TaskFlow.Application.Alerts.Commands.MarkAlertRead;
using TaskFlow.Application.Alerts.Queries.GetBoardAlerts;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class AlertsController(ISender sender) : ControllerBase
{
    /// <summary>Lists alerts raised for a board (optionally only the unread ones).</summary>
    [HttpGet("boards/{boardId:guid}/alerts")]
    [ProducesResponseType(typeof(IReadOnlyList<AlertDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBoardAlerts(
        Guid boardId, [FromQuery] bool unreadOnly, CancellationToken cancellationToken)
    {
        var alerts = await sender.Send(new GetBoardAlertsQuery(boardId, unreadOnly), cancellationToken);
        return Ok(alerts);
    }

    /// <summary>Marks a single alert as read.</summary>
    [HttpPatch("alerts/{alertId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(Guid alertId, CancellationToken cancellationToken)
    {
        await sender.Send(new MarkAlertReadCommand(alertId), cancellationToken);
        return NoContent();
    }

    /// <summary>Creates a new alert rule (threshold) for a board, evaluated by the background worker.</summary>
    [HttpPost("boards/{boardId:guid}/alert-rules")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateAlertRule(
        Guid boardId, CreateAlertRuleRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateAlertRuleCommand(
            boardId, request.RuleType, request.Threshold, request.EvaluationWindowMinutes);

        var ruleId = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetBoardAlerts), new { boardId }, ruleId);
    }
}

public sealed record CreateAlertRuleRequest(AlertRuleType RuleType, int Threshold, int EvaluationWindowMinutes);
