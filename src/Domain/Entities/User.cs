using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Entities;

public class User : Entity
{
    public string Email { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public DateTime CreatedAtUtc { get; private set; }

    private User() { } // EF Core

    private User(string email, string displayName)
    {
        Email = email;
        DisplayName = displayName;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public static Result<User> Create(string email, string displayName)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return Result.Failure<User>("A valid email address is required.");

        if (string.IsNullOrWhiteSpace(displayName))
            return Result.Failure<User>("Display name cannot be empty.");

        return Result.Success(new User(email.Trim().ToLowerInvariant(), displayName.Trim()));
    }

    public void Rename(string newDisplayName)
    {
        if (!string.IsNullOrWhiteSpace(newDisplayName))
            DisplayName = newDisplayName.Trim();
    }
}
