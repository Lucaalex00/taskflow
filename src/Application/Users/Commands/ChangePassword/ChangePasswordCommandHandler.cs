using FluentValidation.Results;
using MediatR;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Application.Users.Commands.ChangePassword;

public sealed class ChangePasswordCommandHandler(
    ITaskFlowDbContext context, ICurrentUserService currentUser, IPasswordHasher passwordHasher)
    : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Users.FindAsync([currentUser.UserId], cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.User), currentUser.UserId);

        // A 400 on the CurrentPassword field, not a 401: the caller *is* authenticated, they
        // just filled one field of a form wrong, and the UI wants to say so next to that field.
        if (!passwordHasher.Verify(user.PasswordHash, request.CurrentPassword))
            throw new ValidationException(
            [
                new ValidationFailure(nameof(request.CurrentPassword), "Current password is incorrect.")
            ]);

        var result = user.ChangePasswordHash(passwordHasher.Hash(request.NewPassword));
        if (!result.IsSuccess)
            throw new ValidationException([new ValidationFailure(nameof(request.NewPassword), result.Error!)]);

        await context.SaveChangesAsync(cancellationToken);
    }
}
