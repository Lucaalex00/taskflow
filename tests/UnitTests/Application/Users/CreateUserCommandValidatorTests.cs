using FluentAssertions;
using TaskFlow.Application.Users.Commands.CreateUser;
using Xunit;

namespace TaskFlow.UnitTests.Application.Users;

public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new();

    [Fact]
    public void Validate_WithAStrongPassword_Passes()
    {
        var command = new CreateUserCommand("ada@example.com", "Ada", "Correct-horse9");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Ab1cde", "too short")]
    [InlineData("alllowercase1", "no uppercase")]
    [InlineData("ALLUPPERCASE1", "no lowercase")]
    [InlineData("NoNumbersHere", "no number")]
    public void Validate_WithAWeakPassword_Fails(string password, string reason)
    {
        var command = new CreateUserCommand("ada@example.com", "Ada", password);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse($"password is invalid: {reason}");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserCommand.Password));
    }
}
