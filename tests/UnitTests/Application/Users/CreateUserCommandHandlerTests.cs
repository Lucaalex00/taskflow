using FluentAssertions;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Users.Commands.CreateUser;
using TaskFlow.UnitTests.Common;
using Xunit;

namespace TaskFlow.UnitTests.Application.Users;

public class CreateUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithANewEmail_CreatesTheUserAndReturnsAToken()
    {
        await using var context = new TestDbContext();
        var handler = new CreateUserCommandHandler(context, new FakePasswordHasher(), new FakeTokenGenerator());
        var command = new CreateUserCommand("ada@example.com", "Ada", "correct-horse-battery-staple");

        var result = await handler.Handle(command, CancellationToken.None);

        result.DisplayName.Should().Be("Ada");
        result.Token.Should().Be($"token-for-{result.UserId}");

        var savedUser = await context.Users.FindAsync(result.UserId);
        savedUser!.PasswordHash.Should().Be("hashed:correct-horse-battery-staple");
    }

    [Fact]
    public async Task Handle_WithAnEmailAlreadyRegistered_ThrowsValidationException()
    {
        await using var context = new TestDbContext();
        var handler = new CreateUserCommandHandler(context, new FakePasswordHasher(), new FakeTokenGenerator());
        await handler.Handle(new CreateUserCommand("ada@example.com", "Ada", "password123"), CancellationToken.None);

        var act = async () => await handler.Handle(
            new CreateUserCommand("Ada@Example.com", "Ada Two", "password456"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }
}
