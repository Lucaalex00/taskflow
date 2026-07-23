using MediatR;

namespace TaskFlow.Application.Users.Queries.GetUsers;

public sealed record GetUsersQuery : IRequest<IReadOnlyList<UserDto>>;
