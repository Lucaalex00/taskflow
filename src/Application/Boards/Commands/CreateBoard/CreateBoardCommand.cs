using MediatR;

namespace TaskFlow.Application.Boards.Commands.CreateBoard;

public sealed record CreateBoardCommand(string Name, string? Color = null) : IRequest<Guid>;
