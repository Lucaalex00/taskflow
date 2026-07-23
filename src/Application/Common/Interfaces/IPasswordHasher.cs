namespace TaskFlow.Application.Common.Interfaces;

/// <summary>
/// Application depends on this abstraction, not on a specific hashing algorithm —
/// Infrastructure implements it (see ADR 0002: Domain has zero framework dependencies).
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string passwordHash, string providedPassword);
}
