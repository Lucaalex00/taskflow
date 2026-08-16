namespace TaskFlow.Infrastructure.Persistence;

/// <summary>Controls the demo workspace seeded on an empty database (see <see cref="DemoDataSeeder"/>).
/// Bound from the "Seed" configuration section / <c>Seed__*</c> environment variables.</summary>
public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>When false (the default outside the Docker demo), the database is left empty.</summary>
    public bool Enabled { get; set; }

    /// <summary>Shared password for every seeded account. Must satisfy the real password policy —
    /// the seeder hashes it through the same <c>IPasswordHasher</c> registration uses, so demo
    /// accounts are ordinary accounts with no special path through login.</summary>
    public string Password { get; set; } = "Demo-password-2026";
}
