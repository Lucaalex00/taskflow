using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Application.Users.Queries.GetUsers;

public sealed class GetUsersQueryHandler(ITaskFlowDbContext context)
    : IRequestHandler<GetUsersQuery, IReadOnlyList<UserDto>>
{
    public async Task<IReadOnlyList<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        return await context.Users
            .AsNoTracking()
            .OrderBy(u => u.DisplayName)
            .Select(u => new UserDto(u.Id, u.DisplayName, u.Email, u.Color))
            .ToListAsync(cancellationToken);
    }
}
