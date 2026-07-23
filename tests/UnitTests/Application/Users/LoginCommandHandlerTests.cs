using FluentAssertions;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Users.Commands.Login;
using TaskFlow.Domain.Entities;
using TaskFlow.UnitTests.Common;
using Xunit;

namespace TaskFlow.UnitTests.Application.Users;

public class LoginCommandHandlerTests
{
    private static async Task<TestDbContext> SeedContextWithUserAsync(string email, string password)
    {
        var context = new TestDbContext();
        var user = User.Create(email, "Ada", $"hashed:{password}").Value;
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return context;
    }

    [Fact]
    public async Task Handle_WithCorrectCredentials_ReturnsAToken()
    {
        await using var context = await SeedContextWithUserAsync("ada@example.com", "correct-horse-battery-staple");
        var handler = new LoginCommandHandler(context, new FakePasswordHasher(), new FakeTokenGenerator());

        var result = await handler.Handle(
            new LoginCommand("ada@example.com", "correct-horse-battery-staple"), CancellationToken.None);

        result.DisplayName.Should().Be("Ada");
        result.Token.Should().Be($"token-for-{result.UserId}");
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ThrowsAuthenticationException()
    {
        await using var context = await SeedContextWithUserAsync("ada@example.com", "correct-horse-battery-staple");
        var handler = new LoginCommandHandler(context, new FakePasswordHasher(), new FakeTokenGenerator());

        var act = async () => await handler.Handle(
            new LoginCommand("ada@example.com", "wrong-password"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationException>();
    }

    [Fact]
    public async Task Handle_WithUnknownEmail_ThrowsAuthenticationException()
    {
        await using var context = new TestDbContext();
        var handler = new LoginCommandHandler(context, new FakePasswordHasher(), new FakeTokenGenerator());

        var act = async () => await handler.Handle(
            new LoginCommand("nobody@example.com", "whatever"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationException>();
    }
}
