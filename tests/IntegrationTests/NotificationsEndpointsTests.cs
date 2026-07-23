using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using TaskFlow.Application.Notifications;
using TaskFlow.Application.Users;
using TaskFlow.Domain.Enums;
using Xunit;

namespace TaskFlow.IntegrationTests;

public class NotificationsEndpointsTests(TaskFlowApiFactory factory) : IClassFixture<TaskFlowApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };

    private async Task<(Guid UserId, string Email, string Token)> RegisterAsync(string displayName)
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var response = await _client.PostAsJsonAsync("/api/users", new
        {
            Email = email,
            DisplayName = displayName,
            Password = "correct-horse-battery-staple"
        });
        var auth = await response.Content.ReadFromJsonAsync<AuthResult>(JsonOptions);
        return (auth!.UserId, email, auth.Token);
    }

    private void AuthenticateAs(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    [Fact]
    public async Task InvitingAnUnregisteredEmail_SurfacesANotificationOnceTheyRegister()
    {
        var (_, _, ownerToken) = await RegisterAsync("Owner");
        AuthenticateAs(ownerToken);
        var boardResponse = await _client.PostAsJsonAsync("/api/boards", new { Name = "Team Board" });
        var boardId = await boardResponse.Content.ReadFromJsonAsync<Guid>();

        var futureEmail = $"{Guid.NewGuid()}@example.com";
        var inviteResponse = await _client.PostAsJsonAsync($"/api/boards/{boardId}/invitations", new { Email = futureEmail });
        inviteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Not registered yet: no notification anywhere.
        var registerResponse = await _client.PostAsJsonAsync("/api/users", new
        {
            Email = futureEmail,
            DisplayName = "Late Joiner",
            Password = "correct-horse-battery-staple"
        });
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResult>(JsonOptions);
        AuthenticateAs(auth!.Token);

        var notifications = await _client.GetFromJsonAsync<List<NotificationDto>>("/api/notifications", JsonOptions);
        notifications.Should().ContainSingle(n => n.Type == NotificationType.BoardInvitation && n.BoardId == boardId);
    }

    [Fact]
    public async Task DecliningAnInvitation_DoesNotCreateBoardMembership()
    {
        var (_, _, ownerToken) = await RegisterAsync("Owner");
        AuthenticateAs(ownerToken);
        var boardResponse = await _client.PostAsJsonAsync("/api/boards", new { Name = "Team Board" });
        var boardId = await boardResponse.Content.ReadFromJsonAsync<Guid>();

        var (_, inviteeEmail, inviteeToken) = await RegisterAsync("Invitee");
        AuthenticateAs(ownerToken);
        await _client.PostAsJsonAsync($"/api/boards/{boardId}/invitations", new { Email = inviteeEmail });

        AuthenticateAs(inviteeToken);
        var notifications = await _client.GetFromJsonAsync<List<NotificationDto>>("/api/notifications", JsonOptions);
        var invitationId = notifications!.Single(n => n.BoardId == boardId).InvitationId;

        var respondResponse = await _client.PostAsJsonAsync($"/api/invitations/{invitationId}/respond", new { Accept = false });
        respondResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var boards = await _client.GetFromJsonAsync<List<object>>("/api/boards");
        boards.Should().BeEmpty();
    }

    [Fact]
    public async Task AssigningATask_NotifiesTheAssignee_ButNotWhenAssigningToSelf()
    {
        var (ownerId, _, ownerToken) = await RegisterAsync("Owner");
        AuthenticateAs(ownerToken);
        var boardResponse = await _client.PostAsJsonAsync("/api/boards", new { Name = "Team Board" });
        var boardId = await boardResponse.Content.ReadFromJsonAsync<Guid>();

        var (memberId, memberEmail, memberToken) = await RegisterAsync("Teammate");
        AuthenticateAs(ownerToken);
        await _client.PostAsJsonAsync($"/api/boards/{boardId}/invitations", new { Email = memberEmail });
        AuthenticateAs(memberToken);
        var invitations = await _client.GetFromJsonAsync<List<NotificationDto>>("/api/notifications", JsonOptions);
        var invitationId = invitations!.Single(n => n.BoardId == boardId).InvitationId;
        await _client.PostAsJsonAsync($"/api/invitations/{invitationId}/respond", new { Accept = true });

        AuthenticateAs(ownerToken);
        var taskResponse = await _client.PostAsJsonAsync($"/api/boards/{boardId}/tasks",
            new { Title = "Ship it", Description = (string?)null, Priority = "Medium", DueAtUtc = (string?)null }, JsonOptions);
        var taskId = await taskResponse.Content.ReadFromJsonAsync<Guid>();

        // Assign to self: no notification.
        await _client.PatchAsJsonAsync($"/api/tasks/{taskId}/assignee", new { UserId = ownerId }, JsonOptions);
        var ownerNotifications = await _client.GetFromJsonAsync<List<NotificationDto>>("/api/notifications", JsonOptions);
        ownerNotifications.Should().NotContain(n => n.Type == NotificationType.TaskAssigned);

        // Reassign to the teammate: they get notified.
        await _client.PatchAsJsonAsync($"/api/tasks/{taskId}/assignee", new { UserId = memberId }, JsonOptions);

        AuthenticateAs(memberToken);
        var memberNotifications = await _client.GetFromJsonAsync<List<NotificationDto>>("/api/notifications", JsonOptions);
        memberNotifications.Should().ContainSingle(n => n.Type == NotificationType.TaskAssigned && n.TaskId == taskId);
    }

    [Fact]
    public async Task MarkNotificationRead_MarksItAsRead()
    {
        var (_, _, ownerToken) = await RegisterAsync("Owner");
        AuthenticateAs(ownerToken);
        var boardResponse = await _client.PostAsJsonAsync("/api/boards", new { Name = "Team Board" });
        var boardId = await boardResponse.Content.ReadFromJsonAsync<Guid>();

        var (_, memberEmail, memberToken) = await RegisterAsync("Teammate");
        AuthenticateAs(ownerToken);
        await _client.PostAsJsonAsync($"/api/boards/{boardId}/invitations", new { Email = memberEmail });

        AuthenticateAs(memberToken);
        var notifications = await _client.GetFromJsonAsync<List<NotificationDto>>("/api/notifications", JsonOptions);
        var notification = notifications!.Single(n => n.BoardId == boardId);
        notification.IsRead.Should().BeFalse();

        var response = await _client.PostAsync($"/api/notifications/{notification.Id}/read", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var updated = await _client.GetFromJsonAsync<List<NotificationDto>>("/api/notifications", JsonOptions);
        updated!.Single(n => n.Id == notification.Id).IsRead.Should().BeTrue();
    }
}
