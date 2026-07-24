using FluentAssertions;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Users.Commands.Login;
using TaskFlow.Domain.Entities;
using TaskFlow.UnitTests.Common;
using Xunit;

namespace TaskFlow.UnitTests.Application.Users;

public class LoginCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static async Task<TestDbContext> SeedContextWithUserAsync(string email, string password)
    {
        var context = new TestDbContext();
        var user = User.Create(email, "Ada", $"hashed:{password}").Value;
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return context;
    }

    private static LoginCommandHandler CreateHandler(TestDbContext context, DateTime? now = null) =>
        new(context, new FakePasswordHasher(), new FakeTokenGenerator(), new FakeDateTimeProvider(now ?? Now));

    [Fact]
    public async Task Handle_WithCorrectCredentials_ReturnsAToken()
    {
        await using var context = await SeedContextWithUserAsync("ada@example.com", "Correct-horse-battery-staple9");
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new LoginCommand("ada@example.com", "Correct-horse-battery-staple9"), CancellationToken.None);

        result.DisplayName.Should().Be("Ada");
        result.Token.Should().Be($"token-for-{result.UserId}");
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ThrowsAuthenticationException()
    {
        await using var context = await SeedContextWithUserAsync("ada@example.com", "Correct-horse-battery-staple9");
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(
            new LoginCommand("ada@example.com", "wrong-password"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationException>();
    }

    [Fact]
    public async Task Handle_WithUnknownEmail_ThrowsAuthenticationException()
    {
        await using var context = new TestDbContext();
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(
            new LoginCommand("nobody@example.com", "whatever"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationException>();
    }

    [Fact]
    public async Task Handle_AfterFiveConsecutiveFailures_LocksTheAccountEvenWithTheCorrectPassword()
    {
        await using var context = await SeedContextWithUserAsync("ada@example.com", "Correct-horse-battery-staple9");
        var handler = CreateHandler(context);

        // Five wrong attempts trip the lockout.
        for (var i = 0; i < 5; i++)
        {
            var wrong = async () => await handler.Handle(
                new LoginCommand("ada@example.com", "wrong-password"), CancellationToken.None);
            await wrong.Should().ThrowAsync<AuthenticationException>();
        }

        // Now even the correct password is refused while the lockout window is active.
        var correctButLocked = async () => await handler.Handle(
            new LoginCommand("ada@example.com", "Correct-horse-battery-staple9"), CancellationToken.None);

        (await correctButLocked.Should().ThrowAsync<AuthenticationException>())
            .Which.Message.Should().Contain("locked");
    }

    [Fact]
    public async Task Handle_OnceTheLockoutWindowPasses_AllowsLoginAgain()
    {
        await using var context = await SeedContextWithUserAsync("ada@example.com", "Correct-horse-battery-staple9");
        var clock = new FakeDateTimeProvider(Now);
        var handler = new LoginCommandHandler(context, new FakePasswordHasher(), new FakeTokenGenerator(), clock);

        for (var i = 0; i < 5; i++)
        {
            var wrong = async () => await handler.Handle(
                new LoginCommand("ada@example.com", "wrong-password"), CancellationToken.None);
            await wrong.Should().ThrowAsync<AuthenticationException>();
        }

        // Advance past the 15-minute lockout window.
        clock.UtcNow = Now.AddMinutes(16);

        var result = await handler.Handle(
            new LoginCommand("ada@example.com", "Correct-horse-battery-staple9"), CancellationToken.None);

        result.DisplayName.Should().Be("Ada");
    }

    [Fact]
    public async Task Handle_ASuccessfulLogin_ResetsTheFailedAttemptCounter()
    {
        await using var context = await SeedContextWithUserAsync("ada@example.com", "Correct-horse-battery-staple9");
        var handler = CreateHandler(context);

        // Four failures — one short of the lockout threshold.
        for (var i = 0; i < 4; i++)
        {
            var wrong = async () => await handler.Handle(
                new LoginCommand("ada@example.com", "wrong-password"), CancellationToken.None);
            await wrong.Should().ThrowAsync<AuthenticationException>();
        }

        // A success resets the counter...
        await handler.Handle(new LoginCommand("ada@example.com", "Correct-horse-battery-staple9"), CancellationToken.None);

        // ...so four more failures still don't lock the account (would need five fresh ones).
        for (var i = 0; i < 4; i++)
        {
            var wrong = async () => await handler.Handle(
                new LoginCommand("ada@example.com", "wrong-password"), CancellationToken.None);
            await wrong.Should().ThrowAsync<AuthenticationException>();
        }

        var stillAllowed = await handler.Handle(
            new LoginCommand("ada@example.com", "Correct-horse-battery-staple9"), CancellationToken.None);

        stillAllowed.DisplayName.Should().Be("Ada");
    }
}
