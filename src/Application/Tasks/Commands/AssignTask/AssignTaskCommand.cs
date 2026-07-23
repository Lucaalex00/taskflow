using MediatR;

namespace TaskFlow.Application.Tasks.Commands.AssignTask;

public sealed record AssignTaskCommand(Guid TaskId, Guid UserId) : IRequest;
