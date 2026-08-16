using MediatR;

namespace TaskFlow.Application.Boards.Commands.ArchiveBoard;

/// <summary>Logically deletes ("archives") a board — it stays in the database with its tasks but
/// disappears from board lists. Owner-only.</summary>
public sealed record ArchiveBoardCommand(Guid BoardId) : IRequest;
