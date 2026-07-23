using MediatR;

namespace TaskFlow.Application.Users.Commands.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResult>;
