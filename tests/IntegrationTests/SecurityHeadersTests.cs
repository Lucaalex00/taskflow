using FluentAssertions;
using Xunit;

namespace TaskFlow.IntegrationTests;

/// <summary>
/// Verifies the defensive security headers are present on API responses (including on the
/// anonymous /health endpoint, i.e. before any auth runs).
/// </summary>
public class SecurityHeadersTests(TaskFlowApiFactory factory) : IClassFixture<TaskFlowApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("X-Content-Type-Options", "nosniff")]
    [InlineData("X-Frame-Options", "DENY")]
    [InlineData("Referrer-Policy", "strict-origin-when-cross-origin")]
    public async Task Response_IncludesSecurityHeader(string header, string expectedValue)
    {
        var response = await _client.GetAsync("/health");

        response.Headers.Should().ContainKey(header);
        response.Headers.GetValues(header).Should().ContainSingle().Which.Should().Be(expectedValue);
    }

    [Fact]
    public async Task Response_IncludesAStrictContentSecurityPolicy()
    {
        var response = await _client.GetAsync("/health");

        response.Headers.Should().ContainKey("Content-Security-Policy");
        response.Headers.GetValues("Content-Security-Policy").Single().Should().Contain("default-src 'none'");
    }
}
