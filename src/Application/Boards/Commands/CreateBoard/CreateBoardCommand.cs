using MediatR;

namespace TaskFlow.Application.Boards.Commands.CreateBoard;

public sealed record CreateBoardCommand(string Name) : IRequest<Guid>;
