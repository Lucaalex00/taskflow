using MediatR;

namespace TaskFlow.Application.Tasks.Queries.GetBoardTasks;

/// <summary>Lists a board's tasks. Archived (logically-deleted) tasks are excluded unless
/// IncludeArchived is set, so the board shows them only on demand.</summary>
public sealed record GetBoardTasksQuery(Guid BoardId, bool IncludeArchived = false)
    : IRequest<IReadOnlyList<TaskDto>>;
