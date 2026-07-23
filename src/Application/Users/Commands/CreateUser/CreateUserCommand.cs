using MediatR;

namespace TaskFlow.Application.Users.Commands.CreateUser;

public sealed record CreateUserCommand(string Email, string DisplayName, string Password) : IRequest<AuthResult>;
