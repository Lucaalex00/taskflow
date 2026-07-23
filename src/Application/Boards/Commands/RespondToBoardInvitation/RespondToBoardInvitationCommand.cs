using MediatR;

namespace TaskFlow.Application.Boards.Commands.RespondToBoardInvitation;

public sealed record RespondToBoardInvitationCommand(Guid InvitationId, bool Accept) : IRequest;
