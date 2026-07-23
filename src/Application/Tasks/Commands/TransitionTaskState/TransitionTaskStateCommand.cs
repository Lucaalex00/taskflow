using MediatR;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Tasks.Commands.TransitionTaskState;

public sealed record TransitionTaskStateCommand(Guid TaskId, TaskState NewState) : IRequest;
