using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Application.Configuration.Queries.GetPublicConfig;

public sealed class GetPublicConfigQueryHandler(ITaskFlowDbContext context, IDemoAccountProvider demoAccount)
    : IRequestHandler<GetPublicConfigQuery, PublicConfigDto>
{
    private static readonly PublicConfigDto NoDemoAccount = new(false, null, null);

    public async Task<PublicConfigDto> Handle(GetPublicConfigQuery request, CancellationToken cancellationToken)
    {
        if (!demoAccount.IsEnabled)
            return NoDemoAccount;

        // Seeding being *enabled* isn't the same as the account existing: the seeder skips a
        // database that already has users, so an instance upgraded from a pre-seed database
        // would otherwise advertise a demo login that fails.
        var email = demoAccount.Email.Trim().ToLowerInvariant();
        var exists = await context.Users.AnyAsync(u => u.Email == email, cancellationToken);

        return exists
            ? new PublicConfigDto(true, demoAccount.Email, demoAccount.Password)
            : NoDemoAccount;
    }
}
