using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using TaskFlow.Application.Boards;
using TaskFlow.Application.Configuration.Queries.GetPublicConfig;
using TaskFlow.Application.Notifications;
using TaskFlow.Application.Tasks;
using TaskFlow.Application.Users;
using TaskFlow.Domain.Enums;
using Xunit;

namespace TaskFlow.IntegrationTests;

/// <summary>
/// Covers the first ninety seconds of a reviewer's experience: the login screen discovers a
/// demo account, one click signs in, and the workspace behind it is genuinely populated —
/// including load that the anomaly detector is configured to flag.
/// </summary>
public class DemoSeedTests(SeededApiFactory factory) : IClassFixture<SeededApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };

    private async Task SignInAsDemoAsync()
    {
        var config = await _client.GetFromJsonAsync<PublicConfigDto>("/api/config", JsonOptions);
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = config!.DemoEmail,
            Password = config.DemoPassword
        });

        var auth = await response.Content.ReadFromJsonAsync<AuthResult>(JsonOptions);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
    }

    [Fact]
    public async Task GetConfig_OnASeededInstance_AdvertisesTheDemoAccountAnonymously()
    {
        var response = await _client.GetAsync("/api/config");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var config = await response.Content.ReadFromJsonAsync<PublicConfigDto>(JsonOptions);
        config!.DemoAccountAvailable.Should().BeTrue();
        config.DemoEmail.Should().Be("demo@taskflow.dev");
        config.DemoPassword.Should().Be(SeededApiFactory.SeedPassword);
    }

    [Fact]
    public async Task TheAdvertisedCredentials_WorkAtTheOrdinaryLoginEndpoint()
    {
        var config = await _client.GetFromJsonAsync<PublicConfigDto>("/api/config", JsonOptions);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = config!.DemoEmail,
            Password = config.DemoPassword
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TheDemoUser_LandsOnPopulatedBoards()
    {
        await SignInAsDemoAsync();

        var boards = await _client.GetFromJsonAsync<List<BoardDto>>("/api/boards", JsonOptions);

        // Two boards they belong to; the third seeded board is only an unanswered invitation.
        boards.Should().HaveCount(2);
        boards.Should().Contain(b => b.Name == "Product launch");
        boards.Should().OnlyContain(b => b.TaskCount > 0);
    }

    [Fact]
    public async Task TheLaunchBoard_CarriesLoadTheAnomalyRulesAreConfiguredToFlag()
    {
        await SignInAsDemoAsync();
        var boards = await _client.GetFromJsonAsync<List<BoardDto>>("/api/boards", JsonOptions);
        var launch = boards!.Single(b => b.Name == "Product launch");

        var tasks = await _client.GetFromJsonAsync<List<TaskDto>>($"/api/boards/{launch.Id}/tasks", JsonOptions);

        // The seeded rules use a threshold of 2 and the evaluators fire on "more than" it, so
        // the same assignee needs at least 3 overdue and 3 in-progress tasks for the first
        // worker cycle to produce a visible alert.
        var now = DateTime.UtcNow;
        var mostOverdue = tasks!
            .Where(t => t.AssigneeId is not null && t.DueAtUtc < now
                        && t.State != TaskState.Done && t.State != TaskState.Cancelled)
            .GroupBy(t => t.AssigneeId)
            .Max(g => g.Count());
        mostOverdue.Should().BeGreaterThan(2);

        var mostInProgress = tasks!
            .Where(t => t.AssigneeId is not null && t.State == TaskState.InProgress)
            .GroupBy(t => t.AssigneeId)
            .Max(g => g.Count());
        mostInProgress.Should().BeGreaterThan(2);
    }

    [Fact]
    public async Task TheDemoUser_HasAPendingInvitationWaitingInTheNotificationBell()
    {
        await SignInAsDemoAsync();

        var notifications = await _client.GetFromJsonAsync<List<NotificationDto>>("/api/notifications", JsonOptions);

        notifications.Should().Contain(n =>
            n.Type == NotificationType.BoardInvitation
            && n.InvitationId != null
            && n.InvitationStatus == InvitationStatus.Pending
            && !n.IsRead);
    }
}
