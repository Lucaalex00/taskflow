using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace TaskFlow.IntegrationTests;

/// <summary>
/// Verifies the anonymous register/login endpoints are throttled per client IP, using a
/// factory with a deliberately low limit (see RateLimitedApiFactory) so the test doesn't need
/// to fire an unrealistic number of requests to trip it.
/// </summary>
public class AuthRateLimitingTests(RateLimitedApiFactory factory) : IClassFixture<RateLimitedApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_BeyondTheConfiguredLimit_Returns429()
    {
        HttpResponseMessage? lastResponse = null;

        // The factory's limit is 3 requests/minute for this partition (client IP) — this loop
        // deliberately exceeds it.
        for (var i = 0; i < 5; i++)
        {
            lastResponse = await _client.PostAsJsonAsync("/api/users", new
            {
                Email = $"{Guid.NewGuid()}@example.com",
                DisplayName = "Rate Limit Tester",
                Password = "correct-horse-battery-staple"
            });
        }

        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
