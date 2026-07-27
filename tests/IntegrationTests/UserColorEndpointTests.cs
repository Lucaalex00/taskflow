using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using TaskFlow.Application.Users;
using Xunit;

namespace TaskFlow.IntegrationTests;

public class UserColorEndpointTests(TaskFlowApiFactory factory) : IClassFixture<TaskFlowApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };

    private async Task AuthenticateAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/users", new
        {
            Email = $"{Guid.NewGuid()}@example.com",
            DisplayName = "Colorful",
            Password = "Correct-horse-battery-staple9"
        });
        var auth = await response.Content.ReadFromJsonAsync<AuthResult>(JsonOptions);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
    }

    [Fact]
    public async Task PatchColor_WithAValidHex_UpdatesAndReturnsTheUser()
    {
        await AuthenticateAsync();

        var response = await _client.PatchAsJsonAsync("/api/users/me/color", new { Color = "#d946ef" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserDto>(JsonOptions);
        user!.Color.Should().Be("#d946ef");
    }

    [Fact]
    public async Task PatchColor_WithAnInvalidHex_Returns400()
    {
        await AuthenticateAsync();

        var response = await _client.PatchAsJsonAsync("/api/users/me/color", new { Color = "purple" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PatchColor_WithoutAuth_Returns401()
    {
        var anonymous = factory.CreateClient();

        var response = await anonymous.PatchAsJsonAsync("/api/users/me/color", new { Color = "#d946ef" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
