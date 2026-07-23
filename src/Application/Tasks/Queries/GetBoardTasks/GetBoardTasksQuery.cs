using MediatR;

namespace TaskFlow.Application.Tasks.Queries.GetBoardTasks;

public sealed record GetBoardTasksQuery(Guid BoardId) : IRequest<IReadOnlyList<TaskDto>>;
