using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TaskFlow.Api.Controllers;
using TaskFlow.Application.Boards;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain.Enums;
using Xunit;

namespace TaskFlow.IntegrationTests;

public class TasksEndpointsTests(TaskFlowApiFactory factory) : IClassFixture<TaskFlowApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task FullFlow_CreateUserBoardAndTask_ThenTransitionState_Succeeds()
    {
        // Arrange: a user and a board are prerequisites for creating a task.
        var userResponse = await _client.PostAsJsonAsync("/api/users",
            new { Email = $"{Guid.NewGuid()}@example.com", DisplayName = "Integration Tester" });
        userResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var userId = await userResponse.Content.ReadFromJsonAsync<Guid>();

        var boardResponse = await _client.PostAsJsonAsync("/api/boards",
            new { Name = "Integration Board", OwnerId = userId });
        boardResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var boardId = await boardResponse.Content.ReadFromJsonAsync<Guid>();

        // Act: create a task on that board.
        var createTaskResponse = await _client.PostAsJsonAsync($"/api/boards/{boardId}/tasks",
            new CreateTaskRequest("Ship the demo", "End-to-end check", TaskPriority.High, null));

        createTaskResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var taskId = await createTaskResponse.Content.ReadFromJsonAsync<Guid>();

        // Assert: it shows up when listing the board's tasks.
        var tasks = await _client.GetFromJsonAsync<List<TaskDto>>($"/api/boards/{boardId}/tasks");
        tasks.Should().ContainSingle(t => t.Id == taskId && t.State == TaskState.Todo);

        // Act: move it through the state machine.
        var transitionResponse = await _client.PatchAsJsonAsync($"/api/tasks/{taskId}/state",
            new TransitionTaskStateRequest(TaskState.InProgress));

        transitionResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var updatedTasks = await _client.GetFromJsonAsync<List<TaskDto>>($"/api/boards/{boardId}/tasks");
        updatedTasks.Should().ContainSingle(t => t.Id == taskId && t.State == TaskState.InProgress);
    }

    [Fact]
    public async Task CreateTask_OnNonExistentBoard_Returns404WithProblemDetails()
    {
        var response = await _client.PostAsJsonAsync($"/api/boards/{Guid.NewGuid()}/tasks",
            new CreateTaskRequest("Orphan", null, TaskPriority.Low, null));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task CreateTask_WithEmptyTitle_Returns400WithValidationErrors()
    {
        var userResponse = await _client.PostAsJsonAsync("/api/users",
            new { Email = $"{Guid.NewGuid()}@example.com", DisplayName = "Validator" });
        var userId = await userResponse.Content.ReadFromJsonAsync<Guid>();

        var boardResponse = await _client.PostAsJsonAsync("/api/boards",
            new { Name = "Validation Board", OwnerId = userId });
        var boardId = await boardResponse.Content.ReadFromJsonAsync<Guid>();

        var response = await _client.PostAsJsonAsync($"/api/boards/{boardId}/tasks",
            new CreateTaskRequest("", null, TaskPriority.Low, null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
