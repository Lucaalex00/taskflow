using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using TaskFlow.Application.Users;
using Xunit;

namespace TaskFlow.IntegrationTests;

/// <summary>
/// Exercises AlertsHub end to end through a real SignalR client connected in-process to the
/// WebApplicationFactory's TestServer — verifies both halves of the hub's authorization: an
/// unauthenticated connection is rejected outright, and an authenticated one can only join
/// boards it's actually a member of.
/// </summary>
public class AlertsHubTests(TaskFlowApiFactory factory) : IClassFixture<TaskFlowApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };

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

    private HubConnection BuildConnection(string? token)
    {
        return new HubConnectionBuilder()
            .WithUrl(new Uri(_client.BaseAddress!, "/hubs/alerts"), options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                if (token is not null)
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                }
            })
            .Build();
    }

    [Fact]
    public async Task Connection_WithoutAToken_IsRejected()
    {
        await using var connection = BuildConnection(token: null);

        var act = async () => await connection.StartAsync();

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Member_CanJoinTheirOwnBoard()
    {
        var (_, ownerToken) = await RegisterAsync("Owner");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var boardResponse = await _client.PostAsJsonAsync("/api/boards", new { Name = "My Board" });
        var boardId = await boardResponse.Content.ReadFromJsonAsync<Guid>();

        await using var connection = BuildConnection(ownerToken);
        await connection.StartAsync();

        var act = async () => await connection.InvokeAsync("JoinBoard", boardId.ToString());

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task NonMember_CannotJoinSomeoneElsesBoard()
    {
        var (_, ownerToken) = await RegisterAsync("Owner");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var boardResponse = await _client.PostAsJsonAsync("/api/boards", new { Name = "Private Board" });
        var boardId = await boardResponse.Content.ReadFromJsonAsync<Guid>();

        var (_, outsiderToken) = await RegisterAsync("Outsider");

        await using var connection = BuildConnection(outsiderToken);
        await connection.StartAsync();

        var act = async () => await connection.InvokeAsync("JoinBoard", boardId.ToString());

        await act.Should().ThrowAsync<HubException>();
    }
}
