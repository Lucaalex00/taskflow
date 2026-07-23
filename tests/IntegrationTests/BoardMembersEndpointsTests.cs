using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using TaskFlow.Application.Boards;
using TaskFlow.Application.Notifications;
using TaskFlow.Application.Users;
using TaskFlow.Domain.Enums;
using Xunit;

namespace TaskFlow.IntegrationTests;

public class BoardMembersEndpointsTests(TaskFlowApiFactory factory) : IClassFixture<TaskFlowApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Registers a fresh user (each call returns its own token — registering doesn't
    /// change which identity _client is currently authenticated as).</summary>
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

    /// <summary>Invites by email (as whoever _client is currently authenticated as) and has the
    /// invitee immediately accept, leaving _client authenticated as the invitee afterward.</summary>
    private async Task AddMemberViaInviteAsync(Guid boardId, string inviteeEmail, string inviteeToken)
    {
        var inviteResponse = await _client.PostAsJsonAsync($"/api/boards/{boardId}/invitations", new { Email = inviteeEmail });
        inviteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        AuthenticateAs(inviteeToken);
        var notifications = await _client.GetFromJsonAsync<List<NotificationDto>>("/api/notifications", JsonOptions);
        var invitationId = notifications!.Single(n => n.BoardId == boardId).InvitationId;

        var respondResponse = await _client.PostAsJsonAsync($"/api/invitations/{invitationId}/respond", new { Accept = true });
        respondResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetBoards_OnlyReturnsBoardsTheCallerIsAMemberOf()
    {
        var (_, _, ownerToken) = await RegisterAsync("Owner");
        AuthenticateAs(ownerToken);
        var boardResponse = await _client.PostAsJsonAsync("/api/boards", new { Name = "Private Board" });
        var boardId = await boardResponse.Content.ReadFromJsonAsync<Guid>();

        var (_, _, outsiderToken) = await RegisterAsync("Outsider");
        AuthenticateAs(outsiderToken);

        var boards = await _client.GetFromJsonAsync<List<BoardDto>>("/api/boards", JsonOptions);
        boards.Should().NotContain(b => b.Id == boardId);

        AuthenticateAs(ownerToken);
        var ownerBoards = await _client.GetFromJsonAsync<List<BoardDto>>("/api/boards", JsonOptions);
        ownerBoards.Should().Contain(b => b.Id == boardId);
    }

    [Fact]
    public async Task NonMember_CannotSeeOrActOnATaskOnSomeoneElsesBoard()
    {
        var (_, _, ownerToken) = await RegisterAsync("Owner");
        AuthenticateAs(ownerToken);
        var boardResponse = await _client.PostAsJsonAsync("/api/boards", new { Name = "Private Board" });
        var boardId = await boardResponse.Content.ReadFromJsonAsync<Guid>();

        var (_, _, outsiderToken) = await RegisterAsync("Outsider");
        AuthenticateAs(outsiderToken);

        var response = await _client.GetAsync($"/api/boards/{boardId}/tasks");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Owner_CanInviteAndRemoveAMember()
    {
        var (_, _, ownerToken) = await RegisterAsync("Owner");
        AuthenticateAs(ownerToken);
        var boardResponse = await _client.PostAsJsonAsync("/api/boards", new { Name = "Team Board" });
        var boardId = await boardResponse.Content.ReadFromJsonAsync<Guid>();

        var (memberId, memberEmail, memberToken) = await RegisterAsync("Teammate");
        AuthenticateAs(ownerToken);
        await AddMemberViaInviteAsync(boardId, memberEmail, memberToken);

        AuthenticateAs(ownerToken);
        var members = await _client.GetFromJsonAsync<List<BoardMemberDto>>($"/api/boards/{boardId}/members", JsonOptions);
        members.Should().Contain(m => m.UserId == memberId && m.Role == BoardRole.Member);

        var removeResponse = await _client.DeleteAsync($"/api/boards/{boardId}/members/{memberId}");
        removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var membersAfterRemoval = await _client.GetFromJsonAsync<List<BoardMemberDto>>($"/api/boards/{boardId}/members", JsonOptions);
        membersAfterRemoval.Should().NotContain(m => m.UserId == memberId);
    }

    [Fact]
    public async Task NonOwnerMember_CannotInviteBoardMembers()
    {
        var (_, _, ownerToken) = await RegisterAsync("Owner");
        AuthenticateAs(ownerToken);
        var boardResponse = await _client.PostAsJsonAsync("/api/boards", new { Name = "Team Board" });
        var boardId = await boardResponse.Content.ReadFromJsonAsync<Guid>();

        var (_, memberEmail, memberToken) = await RegisterAsync("Teammate");
        AuthenticateAs(ownerToken);
        await AddMemberViaInviteAsync(boardId, memberEmail, memberToken);

        AuthenticateAs(memberToken);
        var (_, thirdEmail, _) = await RegisterAsync("Another User");

        var response = await _client.PostAsJsonAsync($"/api/boards/{boardId}/invitations", new { Email = thirdEmail });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Owner_CannotRemoveTheLastOwner()
    {
        var (ownerId, _, ownerToken) = await RegisterAsync("Owner");
        AuthenticateAs(ownerToken);
        var boardResponse = await _client.PostAsJsonAsync("/api/boards", new { Name = "Solo Board" });
        var boardId = await boardResponse.Content.ReadFromJsonAsync<Guid>();

        var response = await _client.DeleteAsync($"/api/boards/{boardId}/members/{ownerId}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Owner_CanPromoteAMemberToOwner()
    {
        var (_, _, ownerToken) = await RegisterAsync("Owner");
        AuthenticateAs(ownerToken);
        var boardResponse = await _client.PostAsJsonAsync("/api/boards", new { Name = "Team Board" });
        var boardId = await boardResponse.Content.ReadFromJsonAsync<Guid>();

        var (memberId, memberEmail, memberToken) = await RegisterAsync("Teammate");
        AuthenticateAs(ownerToken);
        await AddMemberViaInviteAsync(boardId, memberEmail, memberToken);
        AuthenticateAs(ownerToken);

        var response = await _client.PatchAsJsonAsync(
            $"/api/boards/{boardId}/members/{memberId}/role", new { Role = BoardRole.Owner }, JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var members = await _client.GetFromJsonAsync<List<BoardMemberDto>>($"/api/boards/{boardId}/members", JsonOptions);
        members.Should().Contain(m => m.UserId == memberId && m.Role == BoardRole.Owner);
    }

    [Fact]
    public async Task Member_CannotAssignTasksToAnyone()
    {
        var (ownerId, _, ownerToken) = await RegisterAsync("Owner");
        AuthenticateAs(ownerToken);
        var boardResponse = await _client.PostAsJsonAsync("/api/boards", new { Name = "Team Board" });
        var boardId = await boardResponse.Content.ReadFromJsonAsync<Guid>();

        var (_, memberEmail, memberToken) = await RegisterAsync("Teammate");
        AuthenticateAs(ownerToken);
        await AddMemberViaInviteAsync(boardId, memberEmail, memberToken);
        AuthenticateAs(ownerToken);

        var taskResponse = await _client.PostAsJsonAsync($"/api/boards/{boardId}/tasks",
            new { Title = "Ship it", Description = (string?)null, Priority = "Medium", DueAtUtc = (string?)null }, JsonOptions);
        var taskId = await taskResponse.Content.ReadFromJsonAsync<Guid>();

        AuthenticateAs(memberToken);
        var response = await _client.PatchAsJsonAsync(
            $"/api/tasks/{taskId}/assignee", new { UserId = ownerId }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Member_CannotCreateTasks()
    {
        var (_, _, ownerToken) = await RegisterAsync("Owner");
        AuthenticateAs(ownerToken);
        var boardResponse = await _client.PostAsJsonAsync("/api/boards", new { Name = "Team Board" });
        var boardId = await boardResponse.Content.ReadFromJsonAsync<Guid>();

        var (_, memberEmail, memberToken) = await RegisterAsync("Teammate");
        AuthenticateAs(ownerToken);
        await AddMemberViaInviteAsync(boardId, memberEmail, memberToken);

        var response = await _client.PostAsJsonAsync($"/api/boards/{boardId}/tasks",
            new { Title = "Ship it", Description = (string?)null, Priority = "Medium", DueAtUtc = (string?)null }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
