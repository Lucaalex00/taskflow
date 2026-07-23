using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using TaskFlow.Api.Controllers;
using TaskFlow.Application.Boards;
using TaskFlow.Application.Tasks;
using TaskFlow.Application.Users;
using TaskFlow.Domain.Enums;
using Xunit;

namespace TaskFlow.IntegrationTests;

public class TasksEndpointsTests(TaskFlowApiFactory factory) : IClassFixture<TaskFlowApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    // Mirrors the JsonStringEnumConverter registered in Program.cs, so the test client
    // round-trips enums (TaskPriority, TaskState, ...) the same way the real Angular
    // frontend does — as strings, not System.Text.Json's numeric default.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Registers a fresh user and attaches its JWT to _client for subsequent calls —
    /// every board/task/alert endpoint requires authentication.</summary>
    private async Task<Guid> RegisterAndAuthenticateAsync(string displayName)
    {
        var response = await _client.PostAsJsonAsync("/api/users", new
        {
            Email = $"{Guid.NewGuid()}@example.com",
            DisplayName = displayName,
            Password = "correct-horse-battery-staple"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var auth = await response.Content.ReadFromJsonAsync<AuthResult>(JsonOptions);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        return auth.UserId;
    }

    [Fact]
    public async Task FullFlow_CreateUserBoardAndTask_ThenTransitionState_Succeeds()
    {
        // Arrange: a user and a board are prerequisites for creating a task.
        var userId = await RegisterAndAuthenticateAsync("Integration Tester");

        var boardResponse = await _client.PostAsJsonAsync("/api/boards",
            new { Name = "Integration Board", OwnerId = userId });
        boardResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var boardId = await boardResponse.Content.ReadFromJsonAsync<Guid>();

        // Act: create a task on that board.
        var createTaskResponse = await _client.PostAsJsonAsync($"/api/boards/{boardId}/tasks",
            new CreateTaskRequest("Ship the demo", "End-to-end check", TaskPriority.High, null), JsonOptions);

        createTaskResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var taskId = await createTaskResponse.Content.ReadFromJsonAsync<Guid>();

        // Assert: it shows up when listing the board's tasks.
        var tasks = await _client.GetFromJsonAsync<List<TaskDto>>($"/api/boards/{boardId}/tasks", JsonOptions);
        tasks.Should().ContainSingle(t => t.Id == taskId && t.State == TaskState.Todo);

        // Act: move it through the state machine.
        var transitionResponse = await _client.PatchAsJsonAsync($"/api/tasks/{taskId}/state",
            new TransitionTaskStateRequest(TaskState.InProgress), JsonOptions);

        transitionResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var updatedTasks = await _client.GetFromJsonAsync<List<TaskDto>>($"/api/boards/{boardId}/tasks", JsonOptions);
        updatedTasks.Should().ContainSingle(t => t.Id == taskId && t.State == TaskState.InProgress);
    }

    [Fact]
    public async Task CreateTask_OnNonExistentBoard_Returns404WithProblemDetails()
    {
        await RegisterAndAuthenticateAsync("Orphan Task Tester");

        var response = await _client.PostAsJsonAsync($"/api/boards/{Guid.NewGuid()}/tasks",
            new CreateTaskRequest("Orphan", null, TaskPriority.Low, null), JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task CreateTask_WithEmptyTitle_Returns400WithValidationErrors()
    {
        var userId = await RegisterAndAuthenticateAsync("Validator");

        var boardResponse = await _client.PostAsJsonAsync("/api/boards",
            new { Name = "Validation Board", OwnerId = userId });
        var boardId = await boardResponse.Content.ReadFromJsonAsync<Guid>();

        var response = await _client.PostAsJsonAsync($"/api/boards/{boardId}/tasks",
            new CreateTaskRequest("", null, TaskPriority.Low, null), JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BoardEndpoint_WithoutAuthentication_Returns401()
    {
        var response = await _client.GetAsync("/api/boards");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
