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

    /// <summary>Auto-assigned at registration (deterministic per user id) — used for avatar/initials
    /// badges in the UI. Not user-choosable, unlike ProjectBoard's color.</summary>
    public string Color { get; private set; } = null!;

    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Consecutive failed login attempts since the last successful login. Reset to 0
    /// on any successful login. Drives temporary lockout (see <see cref="RegisterFailedLogin"/>).</summary>
    public int FailedLoginAttempts { get; private set; }

    /// <summary>When set and in the future, the account is temporarily locked and logins are
    /// refused even with the correct password. Null once the window passes or on success.</summary>
    public DateTime? LockoutEndUtc { get; private set; }

    private User() { } // EF Core

    private User(string email, string displayName, string passwordHash)
    {
        Email = email;
        DisplayName = displayName;
        PasswordHash = passwordHash;
        Color = ColorPalette.PickFor(Id);
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

    /// <summary>Changes the user's avatar color. Returns a failure result (rather than throwing)
    /// for an invalid hex, so the caller can surface it as a validation error.</summary>
    public Result SetColor(string hexColor)
    {
        if (!ColorPalette.IsValidHex(hexColor))
            return Result.Failure("Color must be a valid hex value like #a855f7.");

        Color = hexColor;
        return Result.Success();
    }

    /// <summary>True while a lockout window is active. Callers must refuse login when this
    /// returns true, regardless of whether the supplied password is correct.</summary>
    public bool IsLockedOut(DateTime nowUtc) => LockoutEndUtc is { } end && end > nowUtc;

    /// <summary>Records a failed login attempt. Once <paramref name="maxAttempts"/> consecutive
    /// failures are reached, the account is locked for <paramref name="lockoutDuration"/> and
    /// the counter resets, so the next window starts fresh after the lockout expires.</summary>
    public void RegisterFailedLogin(DateTime nowUtc, int maxAttempts, TimeSpan lockoutDuration)
    {
        FailedLoginAttempts++;

        if (FailedLoginAttempts >= maxAttempts)
        {
            LockoutEndUtc = nowUtc.Add(lockoutDuration);
            FailedLoginAttempts = 0;
        }
    }

    /// <summary>Clears the failed-attempt counter and any lockout after a successful login.</summary>
    public void RegisterSuccessfulLogin()
    {
        FailedLoginAttempts = 0;
        LockoutEndUtc = null;
    }
}
