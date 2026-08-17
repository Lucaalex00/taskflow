using MediatR;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Application.Users.Commands.UpdateProfile;

public sealed class UpdateProfileCommandHandler(ITaskFlowDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<UpdateProfileCommand, UserDto>
{
    public async Task<UserDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Users.FindAsync([currentUser.UserId], cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.User), currentUser.UserId);

        user.Rename(request.DisplayName);
        await context.SaveChangesAsync(cancellationToken);

        return new UserDto(user.Id, user.DisplayName, user.Email, user.Color);
    }
}
