using MediatR;

namespace TaskFlow.Application.Users.Commands.CreateUser;

public sealed record CreateUserCommand(string Email, string DisplayName) : IRequest<Guid>;
