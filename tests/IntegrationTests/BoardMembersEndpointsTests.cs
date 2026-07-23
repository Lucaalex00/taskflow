using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using TaskFlow.Application.Boards;
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
    private async Task<(Guid UserId, string Token)> RegisterAsync(string displayName)
    {
        var response = await _client.PostAsJsonAsync("/api/users", new
        {
            Email = $"{Guid.NewGuid()}@example.com",
            DisplayName = displayName,
            Password = "correct-horse-battery-staple"
        });
        var auth = await response.Content.ReadFromJsonAsync<AuthResult>(JsonOptions);
        return (auth!.UserId, auth.Token);
    }

    private void AuthenticateAs(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    [Fact]
    public async Task GetBoards_OnlyReturnsBoardsTheCallerIsAMemberOf()
    {
        var (_, ownerToken) = await RegisterAsync("Owner");
        AuthenticateAs(ownerToken);
        var boardResponse = await _client.PostAsJsonAsync("/api/boards", new { Name = "Private Board" });
        var boardId = await boardResponse.Content.ReadFromJsonAsync<Guid>();

        var (_, outsiderToken) = await RegisterAsync("Outsider");
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
        var (_, ownerToken) = await RegisterAsync("Owner");
        AuthenticateAs(ownerToken);
        var boardResponse = await _client.PostAsJsonAsync("/api/boards", new { Name = "Private Board" });
        var boardId = await boardResponse.Content.ReadFromJsonAsync<Guid>();

        var (_, outsiderToken) = await RegisterAsync("Outsider");
        AuthenticateAs(outsiderToken);

        var response = await _client.GetAsync($"/api/boards/{boardId}/tasks");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Owner_CanAddAndRemoveAMember()
    {
        var (_, ownerToken) = await RegisterAsync("Owner");
        AuthenticateAs(ownerToken);
        var boardResponse = await _client.PostAsJsonAsync("/api/boards", new { Name = "Team Board" });
        var boardId = await boardResponse.Content.ReadFromJsonAsync<Guid>();

        var (memberId, _) = await RegisterAsync("Teammate");
        AuthenticateAs(ownerToken);

        var addResponse = await _client.PostAsJsonAsync(
            $"/api/boards/{boardId}/members", new { UserId = memberId, Role = BoardRole.Member }, JsonOptions);
        addResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var members = await _client.GetFromJsonAsync<List<BoardMemberDto>>($"/api/boards/{boardId}/members", JsonOptions);
        members.Should().Contain(m => m.UserId == memberId && m.Role == BoardRole.Member);

        var removeResponse = await _client.DeleteAsync($"/api/boards/{boardId}/members/{memberId}");
        removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var membersAfterRemoval = await _client.GetFromJsonAsync<List<BoardMemberDto>>($"/api/boards/{boardId}/members", JsonOptions);
        membersAfterRemoval.Should().NotContain(m => m.UserId == memberId);
    }

    [Fact]
    public async Task NonOwnerMember_CannotAddBoardMembers()
    {
        var (_, ownerToken) = await RegisterAsync("Owner");
        AuthenticateAs(ownerToken);
        var boardResponse = await _client.PostAsJsonAsync("/api/boards", new { Name = "Team Board" });
        var boardId = await boardResponse.Content.ReadFromJsonAsync<Guid>();

        var (memberId, memberToken) = await RegisterAsync("Teammate");
        AuthenticateAs(ownerToken);
        await _client.PostAsJsonAsync(
            $"/api/boards/{boardId}/members", new { UserId = memberId, Role = BoardRole.Member }, JsonOptions);

        AuthenticateAs(memberToken);
        var (thirdUserId, _) = await RegisterAsync("Another User");

        var response = await _client.PostAsJsonAsync(
            $"/api/boards/{boardId}/members", new { UserId = thirdUserId, Role = BoardRole.Member }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Owner_CannotRemoveTheLastOwner()
    {
        var (ownerId, ownerToken) = await RegisterAsync("Owner");
        AuthenticateAs(ownerToken);
        var boardResponse = await _client.PostAsJsonAsync("/api/boards", new { Name = "Solo Board" });
        var boardId = await boardResponse.Content.ReadFromJsonAsync<Guid>();

        var response = await _client.DeleteAsync($"/api/boards/{boardId}/members/{ownerId}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
