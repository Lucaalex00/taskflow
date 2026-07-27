using MediatR;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Application.Users.Commands.UpdateUserColor;

public sealed class UpdateUserColorCommandHandler(ITaskFlowDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<UpdateUserColorCommand, UserDto>
{
    public async Task<UserDto> Handle(UpdateUserColorCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Users.FindAsync([currentUser.UserId], cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.User), currentUser.UserId);

        var result = user.SetColor(request.Color);
        if (!result.IsSuccess)
            throw new ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(request.Color), result.Error!)
            ]);

        await context.SaveChangesAsync(cancellationToken);

        return new UserDto(user.Id, user.DisplayName, user.Email, user.Color);
    }
}
