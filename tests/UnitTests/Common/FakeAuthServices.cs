using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.UnitTests.Common;

/// <summary>Deterministic stand-ins for the real hashing/token infrastructure, shared by
/// every Application-layer test that exercises registration or login.</summary>
public sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hashed:{password}";
    public bool Verify(string passwordHash, string providedPassword) => passwordHash == $"hashed:{providedPassword}";
}

public sealed class FakeTokenGenerator : IJwtTokenGenerator
{
    public string GenerateToken(User user) => $"token-for-{user.Id}";
}
