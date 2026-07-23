using MediatR;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Tasks;
using TaskFlow.Application.Tasks.Commands.AssignTask;
using TaskFlow.Application.Tasks.Commands.CreateTask;
using TaskFlow.Application.Tasks.Commands.TransitionTaskState;
using TaskFlow.Application.Tasks.Queries.GetBoardTasks;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class TasksController(ISender sender) : ControllerBase
{
    /// <summary>Lists every task on a board, sorted by priority then due date.</summary>
    [HttpGet("boards/{boardId:guid}/tasks")]
    [ProducesResponseType(typeof(IReadOnlyList<TaskDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBoardTasks(Guid boardId, CancellationToken cancellationToken)
    {
        var tasks = await sender.Send(new GetBoardTasksQuery(boardId), cancellationToken);
        return Ok(tasks);
    }

    /// <summary>Creates a new task on a board.</summary>
    [HttpPost("boards/{boardId:guid}/tasks")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        Guid boardId, CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateTaskCommand(
            boardId, request.Title, request.Description, request.Priority, request.DueAtUtc);

        var taskId = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetBoardTasks), new { boardId }, taskId);
    }

    /// <summary>Moves a task to a new state (e.g. Todo -> InProgress -> Done).</summary>
    [HttpPatch("tasks/{taskId:guid}/state")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TransitionState(
        Guid taskId, TransitionTaskStateRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new TransitionTaskStateCommand(taskId, request.NewState), cancellationToken);
        return NoContent();
    }

    /// <summary>Assigns a task to a user.</summary>
    [HttpPatch("tasks/{taskId:guid}/assignee")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Assign(
        Guid taskId, AssignTaskRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new AssignTaskCommand(taskId, request.UserId), cancellationToken);
        return NoContent();
    }
}

public sealed record CreateTaskRequest(string Title, string? Description, TaskPriority Priority, DateTime? DueAtUtc);
public sealed record TransitionTaskStateRequest(TaskState NewState);
public sealed record AssignTaskRequest(Guid UserId);
