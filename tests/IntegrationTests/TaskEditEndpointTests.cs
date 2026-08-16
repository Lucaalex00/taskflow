using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using TaskFlow.Application.Tasks;
using TaskFlow.Application.Users;
using TaskFlow.Domain.Enums;
using Xunit;

namespace TaskFlow.IntegrationTests;

public class TaskEditEndpointTests(TaskFlowApiFactory factory) : IClassFixture<TaskFlowApiFactory>
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
            DisplayName = "Editor",
            Password = "Correct-horse-battery-staple9"
        });
        var auth = await response.Content.ReadFromJsonAsync<AuthResult>(JsonOptions);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
    }

    [Fact]
    public async Task EditingATask_ChangesItsContent()
    {
        await AuthenticateAsync();
        var boardResponse = await _client.PostAsJsonAsync("/api/boards", new { Name = "Edit Board" });
        var boardId = await boardResponse.Content.ReadFromJsonAsync<Guid>();

        var taskResponse = await _client.PostAsJsonAsync($"/api/boards/{boardId}/tasks",
            new { Title = "Typo", Description = (string?)null, Priority = "Low", DueAtUtc = (string?)null }, JsonOptions);
        var taskId = await taskResponse.Content.ReadFromJsonAsync<Guid>();

        var editResponse = await _client.PutAsJsonAsync($"/api/tasks/{taskId}",
            new { Title = "Fixed title", Description = "Now with detail", Priority = "High", DueAtUtc = (string?)null }, JsonOptions);
        editResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var tasks = await _client.GetFromJsonAsync<List<TaskDto>>($"/api/boards/{boardId}/tasks", JsonOptions);
        var updated = tasks!.Single(t => t.Id == taskId);
        updated.Title.Should().Be("Fixed title");
        updated.Description.Should().Be("Now with detail");
        updated.Priority.Should().Be(TaskPriority.High);
    }

    [Fact]
    public async Task EditingATask_WithAnEmptyTitle_Returns400()
    {
        await AuthenticateAsync();
        var boardResponse = await _client.PostAsJsonAsync("/api/boards", new { Name = "Edit Board" });
        var boardId = await boardResponse.Content.ReadFromJsonAsync<Guid>();

        var taskResponse = await _client.PostAsJsonAsync($"/api/boards/{boardId}/tasks",
            new { Title = "Keep me", Description = (string?)null, Priority = "Low", DueAtUtc = (string?)null }, JsonOptions);
        var taskId = await taskResponse.Content.ReadFromJsonAsync<Guid>();

        var editResponse = await _client.PutAsJsonAsync($"/api/tasks/{taskId}",
            new { Title = "", Description = (string?)null, Priority = "Low", DueAtUtc = (string?)null }, JsonOptions);

        editResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
