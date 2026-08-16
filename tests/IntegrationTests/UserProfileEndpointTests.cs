using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using TaskFlow.Application.Users;
using Xunit;

namespace TaskFlow.IntegrationTests;

/// <summary>Full HTTP round-trips for the two "manage my own account" endpoints: renaming
/// yourself and changing your password (including that the new password actually works at
/// the login endpoint afterwards, which is the only assertion that proves the whole change
/// took effect end to end).</summary>
public class UserProfileEndpointTests(TaskFlowApiFactory factory) : IClassFixture<TaskFlowApiFactory>
{
    private const string OriginalPassword = "Correct-horse-battery-staple9";

    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };

    private async Task<string> RegisterAsync(string displayName = "Original Name")
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var response = await _client.PostAsJsonAsync("/api/users", new
        {
            Email = email,
            DisplayName = displayName,
            Password = OriginalPassword
        });

        var auth = await response.Content.ReadFromJsonAsync<AuthResult>(JsonOptions);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return email;
    }

    [Fact]
    public async Task PatchMe_RenamesTheSignedInUser()
    {
        await RegisterAsync();

        var response = await _client.PatchAsJsonAsync("/api/users/me", new { DisplayName = "Renamed Person" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserDto>(JsonOptions);
        user!.DisplayName.Should().Be("Renamed Person");
    }

    [Fact]
    public async Task PatchMe_WithAnEmptyDisplayName_Returns400()
    {
        await RegisterAsync();

        var response = await _client.PatchAsJsonAsync("/api/users/me", new { DisplayName = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PatchMe_WithoutAuth_Returns401()
    {
        var anonymous = factory.CreateClient();

        var response = await anonymous.PatchAsJsonAsync("/api/users/me", new { DisplayName = "Nobody" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostPassword_WithTheCorrectCurrentPassword_ChangesWhatLoginAccepts()
    {
        var email = await RegisterAsync();
        const string newPassword = "Brand-new-passphrase7";

        var change = await _client.PostAsJsonAsync("/api/users/me/password", new
        {
            CurrentPassword = OriginalPassword,
            NewPassword = newPassword
        });
        change.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var withNew = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = newPassword });
        withNew.StatusCode.Should().Be(HttpStatusCode.OK);

        var withOld = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = OriginalPassword });
        withOld.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostPassword_WithTheWrongCurrentPassword_Returns400AndLeavesTheOldOneWorking()
    {
        var email = await RegisterAsync();

        var change = await _client.PostAsJsonAsync("/api/users/me/password", new
        {
            CurrentPassword = "Not-the-right-one3",
            NewPassword = "Brand-new-passphrase7"
        });

        change.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var login = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = OriginalPassword });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostPassword_WithAWeakNewPassword_Returns400()
    {
        await RegisterAsync();

        var response = await _client.PostAsJsonAsync("/api/users/me/password", new
        {
            CurrentPassword = OriginalPassword,
            NewPassword = "weak"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostPassword_WithoutAuth_Returns401()
    {
        var anonymous = factory.CreateClient();

        var response = await anonymous.PostAsJsonAsync("/api/users/me/password", new
        {
            CurrentPassword = OriginalPassword,
            NewPassword = "Brand-new-passphrase7"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
