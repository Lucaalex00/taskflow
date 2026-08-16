using FluentAssertions;
using TaskFlow.Application.Users.Commands.ChangePassword;
using Xunit;

namespace TaskFlow.UnitTests.Application.Users;

public class ChangePasswordCommandValidatorTests
{
    private readonly ChangePasswordCommandValidator _validator = new();

    [Fact]
    public void Validate_WithAStrongDifferentNewPassword_Passes()
    {
        var result = _validator.Validate(new ChangePasswordCommand("Old-password-1", "New-password-2"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("short1A")]          // under the 10-character minimum
    [InlineData("all-lowercase-1")]  // no uppercase
    [InlineData("ALL-UPPERCASE-1")]  // no lowercase
    [InlineData("No-digits-here")]   // no number
    public void Validate_AppliesTheSamePasswordPolicyAsRegistration(string newPassword)
    {
        var result = _validator.Validate(new ChangePasswordCommand("Old-password-1", newPassword));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithoutTheCurrentPassword_Fails()
    {
        var result = _validator.Validate(new ChangePasswordCommand("", "New-password-2"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenTheNewPasswordEqualsTheCurrentOne_Fails()
    {
        var result = _validator.Validate(new ChangePasswordCommand("Same-password-1", "Same-password-1"));

        result.IsValid.Should().BeFalse();
    }
}
