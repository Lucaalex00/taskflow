using FluentAssertions;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Entities;
using Xunit;

namespace TaskFlow.UnitTests.Domain;

public class UserTests
{
    [Theory]
    [InlineData("", "Alice", "hash")]
    [InlineData("not-an-email", "Alice", "hash")]
    [InlineData("alice@example.com", "", "hash")]
    [InlineData("alice@example.com", "   ", "hash")]
    [InlineData("alice@example.com", "Alice", "")]
    [InlineData("alice@example.com", "Alice", "   ")]
    public void Create_WithInvalidInput_ReturnsFailure(string email, string displayName, string passwordHash)
    {
        var result = User.Create(email, displayName, passwordHash);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Create_WithValidInput_NormalizesEmailToLowerCase()
    {
        var result = User.Create("Alice.Smith@Example.COM", "Alice Smith", "hash");

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("alice.smith@example.com");
    }

    [Fact]
    public void Create_AssignsAColorFromThePalette()
    {
        var result = User.Create("alice@example.com", "Alice", "hash");

        result.IsSuccess.Should().BeTrue();
        ColorPalette.Colors.Should().Contain(result.Value.Color);
    }

    [Fact]
    public void SetColor_WithAValidHex_UpdatesTheColor()
    {
        var user = User.Create("alice@example.com", "Alice", "hash").Value;

        var result = user.SetColor("#123abc");

        result.IsSuccess.Should().BeTrue();
        user.Color.Should().Be("#123abc");
    }

    [Theory]
    [InlineData("123abc")]     // missing #
    [InlineData("#fff")]        // too short
    [InlineData("#gggggg")]     // not hex
    [InlineData("")]
    public void SetColor_WithAnInvalidHex_FailsAndLeavesTheColorUnchanged(string invalid)
    {
        var user = User.Create("alice@example.com", "Alice", "hash").Value;
        var original = user.Color;

        var result = user.SetColor(invalid);

        result.IsSuccess.Should().BeFalse();
        user.Color.Should().Be(original);
    }
}
