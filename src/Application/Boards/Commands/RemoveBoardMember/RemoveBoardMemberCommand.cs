using MediatR;

namespace TaskFlow.Application.Boards.Commands.RemoveBoardMember;

public sealed record RemoveBoardMemberCommand(Guid BoardId, Guid UserId) : IRequest;
