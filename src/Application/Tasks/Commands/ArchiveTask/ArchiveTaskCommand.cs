using MediatR;

namespace TaskFlow.Application.Tasks.Commands.ArchiveTask;

/// <summary>Logically deletes ("archives") a completed task — it stays in the database but is
/// hidden from the board. Owner-only.</summary>
public sealed record ArchiveTaskCommand(Guid TaskId) : IRequest;
