using MediatR;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Boards.Commands.AddBoardMember;

public sealed record AddBoardMemberCommand(Guid BoardId, Guid UserId, BoardRole Role) : IRequest;
