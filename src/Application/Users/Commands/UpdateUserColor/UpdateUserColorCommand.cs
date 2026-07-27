using MediatR;

namespace TaskFlow.Application.Users.Commands.UpdateUserColor;

/// <summary>Changes the current user's avatar color. The user is taken from the auth context,
/// never from the request, so nobody can recolor someone else's avatar.</summary>
public sealed record UpdateUserColorCommand(string Color) : IRequest<UserDto>;
