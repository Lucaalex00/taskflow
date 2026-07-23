using MediatR;

namespace TaskFlow.Application.Boards.Commands.InviteBoardMember;

public sealed record InviteBoardMemberCommand(Guid BoardId, string Email) : IRequest;
