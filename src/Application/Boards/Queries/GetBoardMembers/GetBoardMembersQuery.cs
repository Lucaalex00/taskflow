using MediatR;

namespace TaskFlow.Application.Boards.Queries.GetBoardMembers;

public sealed record GetBoardMembersQuery(Guid BoardId) : IRequest<IReadOnlyList<BoardMemberDto>>;
