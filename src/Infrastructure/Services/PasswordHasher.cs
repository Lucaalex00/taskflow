using System.Security.Cryptography;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Infrastructure.Services;

/// <summary>
/// PBKDF2-HMAC-SHA256 with a random per-password salt, 100k iterations (OWASP-recommended
/// floor as of this writing). Self-contained rather than delegating to ASP.NET Core Identity's
/// PasswordHasher&lt;TUser&gt; because that requires a live TUser instance to hash against,
/// which doesn't fit Domain's private-constructor entities.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltSizeBytes = 16;
    private const int KeySizeBytes = 32;
    private const int Iterations = 100_000;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySizeBytes);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    public bool Verify(string passwordHash, string providedPassword)
    {
        var parts = passwordHash.Split('.');
        if (parts.Length != 2)
            return false;

        var salt = Convert.FromBase64String(parts[0]);
        var expectedKey = Convert.FromBase64String(parts[1]);
        var providedKey = Rfc2898DeriveBytes.Pbkdf2(
            providedPassword, salt, Iterations, HashAlgorithmName.SHA256, expectedKey.Length);

        return CryptographicOperations.FixedTimeEquals(expectedKey, providedKey);
    }
}
