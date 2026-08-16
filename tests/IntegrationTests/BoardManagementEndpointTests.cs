using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using TaskFlow.Application.Boards;
using TaskFlow.Application.Users;
using Xunit;

namespace TaskFlow.IntegrationTests;

public class BoardManagementEndpointTests(TaskFlowApiFactory factory) : IClassFixture<TaskFlowApiFactory>
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
            DisplayName = "Manager",
            Password = "Correct-horse-battery-staple9"
        });
        var auth = await response.Content.ReadFromJsonAsync<AuthResult>(JsonOptions);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
    }

    [Fact]
    public async Task RenamingABoard_ChangesItsNameInTheList()
    {
        await AuthenticateAsync();
        var boardResponse = await _client.PostAsJsonAsync("/api/boards", new { Name = "First name" });
        var boardId = await boardResponse.Content.ReadFromJsonAsync<Guid>();

        var renameResponse = await _client.PatchAsJsonAsync($"/api/boards/{boardId}/name", new { Name = "Second name" });
        renameResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var boards = await _client.GetFromJsonAsync<List<BoardDto>>("/api/boards", JsonOptions);
        boards!.Single(b => b.Id == boardId).Name.Should().Be("Second name");
    }

    [Fact]
    public async Task ArchivingABoard_RemovesItFromTheList()
    {
        await AuthenticateAsync();
        var boardResponse = await _client.PostAsJsonAsync("/api/boards", new { Name = "Doomed" });
        var boardId = await boardResponse.Content.ReadFromJsonAsync<Guid>();

        var archiveResponse = await _client.PatchAsync($"/api/boards/{boardId}/archive", null);
        archiveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var boards = await _client.GetFromJsonAsync<List<BoardDto>>("/api/boards", JsonOptions);
        boards.Should().NotContain(b => b.Id == boardId);
    }

    [Fact]
    public async Task RenamingABoard_WithAnEmptyName_Returns400()
    {
        await AuthenticateAsync();
        var boardResponse = await _client.PostAsJsonAsync("/api/boards", new { Name = "Keep" });
        var boardId = await boardResponse.Content.ReadFromJsonAsync<Guid>();

        var renameResponse = await _client.PatchAsJsonAsync($"/api/boards/{boardId}/name", new { Name = "" });

        renameResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
