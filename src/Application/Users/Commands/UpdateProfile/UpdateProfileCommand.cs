using MediatR;

namespace TaskFlow.Application.Users.Commands.UpdateProfile;

/// <summary>Renames the signed-in user. Like every "me" command, the identity comes from the
/// auth context, never from the request body, so nobody can rename someone else.</summary>
public sealed record UpdateProfileCommand(string DisplayName) : IRequest<UserDto>;
