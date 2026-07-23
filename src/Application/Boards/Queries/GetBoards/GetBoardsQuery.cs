using MediatR;

namespace TaskFlow.Application.Boards.Queries.GetBoards;

public sealed record GetBoardsQuery : IRequest<IReadOnlyList<BoardDto>>;
