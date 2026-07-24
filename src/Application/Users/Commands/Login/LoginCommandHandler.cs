using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Application.Users.Commands.Login;

public sealed class LoginCommandHandler(
    ITaskFlowDbContext context,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator tokenGenerator,
    IDateTimeProvider clock)
    : IRequestHandler<LoginCommand, AuthResult>
{
    private const string InvalidCredentialsMessage = "Invalid email or password.";
    private const string LockedOutMessage =
        "This account is temporarily locked after too many failed login attempts. Try again later.";

    // After this many consecutive failures, the account is locked for the duration below. Kept
    // conservative so a legitimate user fat-fingering their password a few times isn't punished,
    // but a scripted guessing attack against one account is throttled hard — complementing the
    // per-IP rate limiter (which an attacker rotating IPs could otherwise sidestep).
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public async Task<AuthResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken)
            ?? throw new AuthenticationException(InvalidCredentialsMessage);

        if (user.IsLockedOut(clock.UtcNow))
            throw new AuthenticationException(LockedOutMessage);

        if (!passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            user.RegisterFailedLogin(clock.UtcNow, MaxFailedAttempts, LockoutDuration);
            await context.SaveChangesAsync(cancellationToken);
            throw new AuthenticationException(InvalidCredentialsMessage);
        }

        user.RegisterSuccessfulLogin();
        await context.SaveChangesAsync(cancellationToken);

        return new AuthResult(user.Id, user.DisplayName, user.Color, tokenGenerator.GenerateToken(user));
    }
}
