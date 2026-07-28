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

public class TaskArchiveEndpointTests(TaskFlowApiFactory factory) : IClassFixture<TaskFlowApiFactory>
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
            DisplayName = "Archiver",
            Password = "Correct-horse-battery-staple9"
        });
        var auth = await response.Content.ReadFromJsonAsync<AuthResult>(JsonOptions);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
    }

    [Fact]
    public async Task ArchivingADoneTask_HidesItFromTheBoardUnlessIncludeArchivedIsSet()
    {
        await AuthenticateAsync();
        var boardResponse = await _client.PostAsJsonAsync("/api/boards", new { Name = "Archive Board" });
        var boardId = await boardResponse.Content.ReadFromJsonAsync<Guid>();

        var taskResponse = await _client.PostAsJsonAsync($"/api/boards/{boardId}/tasks",
            new { Title = "Close me", Description = (string?)null, Priority = "Medium", DueAtUtc = (string?)null }, JsonOptions);
        var taskId = await taskResponse.Content.ReadFromJsonAsync<Guid>();

        // Move it to Done (Todo → InProgress → Done).
        await _client.PatchAsJsonAsync($"/api/tasks/{taskId}/state", new { NewState = TaskState.InProgress }, JsonOptions);
        await _client.PatchAsJsonAsync($"/api/tasks/{taskId}/state", new { NewState = TaskState.Done }, JsonOptions);

        // Archive it.
        var archiveResponse = await _client.PatchAsync($"/api/tasks/{taskId}/archive", null);
        archiveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Default list excludes it...
        var tasks = await _client.GetFromJsonAsync<List<TaskDto>>($"/api/boards/{boardId}/tasks", JsonOptions);
        tasks.Should().NotContain(t => t.Id == taskId);

        // ...but includeArchived=true brings it back, flagged archived.
        var withArchived = await _client.GetFromJsonAsync<List<TaskDto>>(
            $"/api/boards/{boardId}/tasks?includeArchived=true", JsonOptions);
        withArchived.Should().ContainSingle(t => t.Id == taskId).Which.IsArchived.Should().BeTrue();
    }

    [Fact]
    public async Task ArchivingANonDoneTask_Returns400()
    {
        await AuthenticateAsync();
        var boardResponse = await _client.PostAsJsonAsync("/api/boards", new { Name = "Archive Board" });
        var boardId = await boardResponse.Content.ReadFromJsonAsync<Guid>();

        var taskResponse = await _client.PostAsJsonAsync($"/api/boards/{boardId}/tasks",
            new { Title = "Still open", Description = (string?)null, Priority = "Low", DueAtUtc = (string?)null }, JsonOptions);
        var taskId = await taskResponse.Content.ReadFromJsonAsync<Guid>();

        var archiveResponse = await _client.PatchAsync($"/api/tasks/{taskId}/archive", null);

        archiveResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
