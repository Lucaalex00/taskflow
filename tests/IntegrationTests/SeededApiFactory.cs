using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Xunit;

namespace TaskFlow.IntegrationTests;

/// <summary>
/// Same shape as <see cref="TaskFlowApiFactory"/>, but with the demo seeder switched on — the
/// configuration the Docker demo actually runs with. Used to prove that what a reviewer sees
/// after `docker compose up` really is there, against a real Postgres and real migrations.
/// </summary>
public sealed class SeededApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string SeedPassword = "Seeded-password-2026";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("taskflow_seeded")
        .WithUsername("taskflow")
        .WithPassword("taskflow")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = _postgres.GetConnectionString(),
                ["LoadMonitor:IntervalSeconds"] = "3600", // don't let the worker interfere mid-test
                ["RateLimiting:Auth:PermitLimit"] = "1000",
                ["Seed:Enabled"] = "true",
                ["Seed:Password"] = SeedPassword,
                // Matches the Docker demo's weekly refresh. Nothing here ages a workspace past
                // it by accident, so the reset only fires in the test that asks for it.
                ["Seed:ResetIntervalHours"] = "168"
            });
        });
    }

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public new async Task DisposeAsync() => await _postgres.DisposeAsync();
}
