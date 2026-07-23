using FluentAssertions;
using TaskFlow.Domain.Entities;
using Xunit;

namespace TaskFlow.UnitTests.Domain;

public class UserTests
{
    [Theory]
    [InlineData("", "Alice")]
    [InlineData("not-an-email", "Alice")]
    [InlineData("alice@example.com", "")]
    [InlineData("alice@example.com", "   ")]
    public void Create_WithInvalidInput_ReturnsFailure(string email, string displayName)
    {
        var result = User.Create(email, displayName);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Create_WithValidInput_NormalizesEmailToLowerCase()
    {
        var result = User.Create("Alice.Smith@Example.COM", "Alice Smith");

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("alice.smith@example.com");
    }
}
