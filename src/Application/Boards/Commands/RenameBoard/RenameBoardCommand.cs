using MediatR;

namespace TaskFlow.Application.Boards.Commands.RenameBoard;

/// <summary>Renames a board. Owner-only.</summary>
public sealed record RenameBoardCommand(Guid BoardId, string Name) : IRequest;
