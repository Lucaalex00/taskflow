using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Entities;

public class User : Entity
{
    public string Email { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;

    /// <summary>
    /// Opaque, already-hashed credential. Domain never sees a plaintext password or hashing
    /// algorithm — that's an infrastructure concern (see IPasswordHasher) — it just stores
    /// and compares whatever hash it's given.
    /// </summary>
    public string PasswordHash { get; private set; } = null!;

    public DateTime CreatedAtUtc { get; private set; }

    private User() { } // EF Core

    private User(string email, string displayName, string passwordHash)
    {
        Email = email;
        DisplayName = displayName;
        PasswordHash = passwordHash;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public static Result<User> Create(string email, string displayName, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return Result.Failure<User>("A valid email address is required.");

        if (string.IsNullOrWhiteSpace(displayName))
            return Result.Failure<User>("Display name cannot be empty.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            return Result.Failure<User>("A password is required.");

        return Result.Success(new User(email.Trim().ToLowerInvariant(), displayName.Trim(), passwordHash));
    }

    public void Rename(string newDisplayName)
    {
        if (!string.IsNullOrWhiteSpace(newDisplayName))
            DisplayName = newDisplayName.Trim();
    }
}
