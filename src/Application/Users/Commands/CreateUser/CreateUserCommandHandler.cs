using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Users.Commands.CreateUser;

public sealed class CreateUserCommandHandler(
    ITaskFlowDbContext context, IPasswordHasher passwordHasher, IJwtTokenGenerator tokenGenerator)
    : IRequestHandler<CreateUserCommand, AuthResult>
{
    public async Task<AuthResult> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var emailTaken = await context.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (emailTaken)
            throw new Common.Exceptions.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(request.Email), "This email is already registered.")
            ]);

        var result = User.Create(request.Email, request.DisplayName, passwordHasher.Hash(request.Password));

        if (!result.IsSuccess)
            throw new Common.Exceptions.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(request.Email), result.Error)
            ]);

        context.Users.Add(result.Value);
        await context.SaveChangesAsync(cancellationToken);

        return new AuthResult(result.Value.Id, result.Value.DisplayName, tokenGenerator.GenerateToken(result.Value));
    }
}
