using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Xunit;

namespace TaskFlow.IntegrationTests;

/// <summary>
/// Same shape as <see cref="TaskFlowApiFactory"/>, but keeps the real (low) auth rate limit
/// from appsettings.json instead of relaxing it — used only by the test that verifies the
/// limiter actually rejects requests once the limit is hit.
/// </summary>
public sealed class RateLimitedApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("taskflow_test")
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
                ["LoadMonitor:IntervalSeconds"] = "3600",
                ["RateLimiting:Auth:PermitLimit"] = "3",
                ["RateLimiting:Auth:WindowSeconds"] = "60"
            });
        });
    }

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public new async Task DisposeAsync() => await _postgres.DisposeAsync();
}
