using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using TaskFlow.Application.Users;
using Xunit;

namespace TaskFlow.IntegrationTests;

/// <summary>
/// Verifies the per-account lockout: after enough failed logins for one account, even the
/// correct password is refused for a while. Complements the per-IP rate limiter — this
/// protects a specific account against an attacker who rotates IPs.
/// </summary>
public class AccountLockoutTests(TaskFlowApiFactory factory) : IClassFixture<TaskFlowApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };

    private const string Password = "Correct-horse-battery-staple9";

    [Fact]
    public async Task Login_AfterFiveFailedAttempts_LocksTheAccountEvenWithTheCorrectPassword()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var register = await _client.PostAsJsonAsync("/api/users", new
        {
            Email = email,
            DisplayName = "Lockout Tester",
            Password
        });
        register.StatusCode.Should().Be(HttpStatusCode.Created);

        // Five wrong-password logins trip the lockout.
        for (var i = 0; i < 5; i++)
        {
            var wrong = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "wrong-password" });
            wrong.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // The correct password is now refused while the lockout window is active.
        var lockedOut = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password });
        lockedOut.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await lockedOut.Content.ReadAsStringAsync();
        body.Should().Contain("locked");
    }

    [Fact]
    public async Task Login_WithFewerThanFiveFailures_ThenTheCorrectPassword_StillSucceeds()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        await _client.PostAsJsonAsync("/api/users", new { Email = email, DisplayName = "Recoverer", Password });

        // Four failures — under the threshold.
        for (var i = 0; i < 4; i++)
        {
            await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "wrong-password" });
        }

        var success = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password });
        success.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await success.Content.ReadFromJsonAsync<AuthResult>(JsonOptions);
        auth!.Token.Should().NotBeNullOrEmpty();
    }
}
