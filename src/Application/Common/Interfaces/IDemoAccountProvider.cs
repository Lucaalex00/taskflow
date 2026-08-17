namespace TaskFlow.Application.Common.Interfaces;

/// <summary>
/// Exposes the credentials of the seeded demo account, when the deployment has one.
/// Infrastructure implements it from the same options the seeder uses, so the two can never
/// disagree about which account exists or what its password is.
/// </summary>
public interface IDemoAccountProvider
{
    /// <summary>False for any deployment that didn't seed a demo workspace.</summary>
    bool IsEnabled { get; }

    string Email { get; }
    string Password { get; }
}
