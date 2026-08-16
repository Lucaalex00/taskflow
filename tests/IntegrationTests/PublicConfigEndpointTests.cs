using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using TaskFlow.Application.Configuration.Queries.GetPublicConfig;
using Xunit;

namespace TaskFlow.IntegrationTests;

/// <summary>The unseeded case: an instance running with the defaults (SEED_DEMO off) must not
/// advertise a demo account, so the login screen shows nothing but the real form.</summary>
public class PublicConfigEndpointTests(TaskFlowApiFactory factory) : IClassFixture<TaskFlowApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task GetConfig_WithoutSeeding_ReportsNoDemoAccountAndLeaksNoCredentials()
    {
        var anonymous = factory.CreateClient();

        var response = await anonymous.GetAsync("/api/config");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var config = await response.Content.ReadFromJsonAsync<PublicConfigDto>(JsonOptions);
        config!.DemoAccountAvailable.Should().BeFalse();
        config.DemoEmail.Should().BeNull();
        config.DemoPassword.Should().BeNull();
    }
}
