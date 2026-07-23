using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Application.Users.Commands.Login;

public sealed class LoginCommandHandler(
    ITaskFlowDbContext context, IPasswordHasher passwordHasher, IJwtTokenGenerator tokenGenerator)
    : IRequestHandler<LoginCommand, AuthResult>
{
    private const string InvalidCredentialsMessage = "Invalid email or password.";

    public async Task<AuthResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken)
            ?? throw new AuthenticationException(InvalidCredentialsMessage);

        if (!passwordHasher.Verify(user.PasswordHash, request.Password))
            throw new AuthenticationException(InvalidCredentialsMessage);

        return new AuthResult(user.Id, user.DisplayName, user.Color, tokenGenerator.GenerateToken(user));
    }
}
