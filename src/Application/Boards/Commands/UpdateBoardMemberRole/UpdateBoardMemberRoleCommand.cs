using MediatR;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Boards.Commands.UpdateBoardMemberRole;

public sealed record UpdateBoardMemberRoleCommand(Guid BoardId, Guid UserId, BoardRole NewRole) : IRequest;
