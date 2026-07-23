using MediatR;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Tasks.Commands.CreateTask;

public sealed record CreateTaskCommand(
    Guid BoardId,
    string Title,
    string? Description,
    TaskPriority Priority,
    DateTime? DueAtUtc) : IRequest<Guid>;
