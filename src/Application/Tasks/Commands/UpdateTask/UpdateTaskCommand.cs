using MediatR;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Tasks.Commands.UpdateTask;

/// <summary>Edits a task's content (title, description, priority, due date). Owner-only.</summary>
public sealed record UpdateTaskCommand(
    Guid TaskId, string Title, string? Description, TaskPriority Priority, DateTime? DueAtUtc) : IRequest;
