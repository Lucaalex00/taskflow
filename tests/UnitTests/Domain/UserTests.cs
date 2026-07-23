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
}
