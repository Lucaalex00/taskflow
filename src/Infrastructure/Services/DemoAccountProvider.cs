using Microsoft.Extensions.Options;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Services;

/// <summary>Reads the demo account straight off the seeder's own options, so the login screen
/// can never advertise an account (or a password) the seeder didn't create.</summary>
public sealed class DemoAccountProvider(IOptions<SeedOptions> options) : IDemoAccountProvider
{
    public bool IsEnabled => options.Value.Enabled;
    public string Email => DemoDataSeeder.OwnerEmail;
    public string Password => options.Value.Password;
}
