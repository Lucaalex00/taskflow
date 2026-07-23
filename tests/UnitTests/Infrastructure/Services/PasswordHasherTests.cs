using FluentAssertions;
using TaskFlow.Infrastructure.Services;
using Xunit;

namespace TaskFlow.UnitTests.Infrastructure.Services;

public class PasswordHasherTests
{
    [Fact]
    public void Verify_WithTheCorrectPassword_ReturnsTrue()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.Hash("correct-horse-battery-staple");

        hasher.Verify(hash, "correct-horse-battery-staple").Should().BeTrue();
    }

    [Fact]
    public void Verify_WithTheWrongPassword_ReturnsFalse()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.Hash("correct-horse-battery-staple");

        hasher.Verify(hash, "wrong-password").Should().BeFalse();
    }

    [Fact]
    public void Hash_ProducesADifferentHashEachTime_DueToRandomSalt()
    {
        var hasher = new PasswordHasher();

        var first = hasher.Hash("same-password");
        var second = hasher.Hash("same-password");

        first.Should().NotBe(second);
        hasher.Verify(first, "same-password").Should().BeTrue();
        hasher.Verify(second, "same-password").Should().BeTrue();
    }
}
