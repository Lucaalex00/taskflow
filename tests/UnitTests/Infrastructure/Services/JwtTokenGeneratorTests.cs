using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Options;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Services;
using Xunit;

namespace TaskFlow.UnitTests.Infrastructure.Services;

public class JwtTokenGeneratorTests
{
    private static JwtTokenGenerator CreateGenerator() => new(Options.Create(new JwtOptions
    {
        Secret = "unit-test-signing-secret-at-least-32-bytes-long",
        Issuer = "TaskFlow.Tests",
        Audience = "TaskFlow.Tests",
        ExpiryMinutes = 60
    }));

    [Fact]
    public void GenerateToken_EmbedsTheUsersIdEmailAndDisplayNameAsClaims()
    {
        var generator = CreateGenerator();
        var user = User.Create("ada@example.com", "Ada", "irrelevant-hash").Value;

        var token = generator.GenerateToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Subject.Should().Be(user.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "ada@example.com");
        jwt.Claims.Should().Contain(c => c.Type == "displayName" && c.Value == "Ada");
        jwt.Issuer.Should().Be("TaskFlow.Tests");
    }
}
