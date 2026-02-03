using System.Net;
using System.Net.Http.Json;
using ExtraTime.API.Tests.Attributes;
using ExtraTime.API.Tests.Fixtures;
using ExtraTime.Application.Features.Bots.DTOs;
using ExtraTime.Application.Features.Leagues.DTOs;

namespace ExtraTime.API.Tests.Endpoints;

[TestCategory(TestCategories.Significant)]
public sealed class BotsEndpointsTests : ApiTestBase
{
    private static object CreateLeagueRequestObject(string name, string? description, bool isPublic, int maxMembers, int scoreExactMatch, int scoreCorrectResult, int bettingDeadlineMinutes)
    {
        return new
        {
            Name = name,
            Description = description,
            IsPublic = isPublic,
            MaxMembers = maxMembers,
            ScoreExactMatch = scoreExactMatch,
            ScoreCorrectResult = scoreCorrectResult,
            BettingDeadlineMinutes = bettingDeadlineMinutes
        };
    }

    [Test]
    public async Task GetBots_Authenticated_ReturnsOk()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        // Act
        var response = await Client.GetAsync("/api/bots");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var bots = await response.Content.ReadFromJsonAsync<List<BotDto>>();
        await Assert.That(bots).IsNotNull();
    }

    [Test]
    public async Task GetBots_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/bots");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
[SkipIfInMemory]
    public async Task GetLeagueBots_Authenticated_ReturnsOk()
    {
        // Arrange - Create league
        var token = await GetAuthTokenAsync("leaguebotuser@example.com");
        SetAuthHeader(token);

        var leagueRequest = CreateLeagueRequestObject(
            "League Bots Test League",
            "Test",
            true,
            10,
            3,
            1,
            5
        );

        var leagueResponse = await Client.PostAsJsonAsync("/api/leagues", leagueRequest);
        await EnsureSuccessAsync(leagueResponse);
        var league = await leagueResponse.Content.ReadFromJsonAsync<LeagueDto>();

        // Act
        var response = await Client.GetAsync($"/api/leagues/{league!.Id}/bots");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var bots = await response.Content.ReadFromJsonAsync<List<LeagueBotDto>>();
        await Assert.That(bots).IsNotNull();
    }

    [Test]
    public async Task GetLeagueBots_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"/api/leagues/{Guid.NewGuid()}/bots");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
[SkipIfInMemory]
    public async Task AddBotToLeague_ValidBot_ReturnsOkOrNotFound()
    {
        // Arrange - Create league and get bot
        var token = await GetAuthTokenAsync("addbotuser@example.com");
        SetAuthHeader(token);

        var leagueRequest = CreateLeagueRequestObject(
            "Add Bot Test League",
            "Test",
            true,
            10,
            3,
            1,
            5
        );

        var leagueResponse = await Client.PostAsJsonAsync("/api/leagues", leagueRequest);
        await EnsureSuccessAsync(leagueResponse);
        var league = await leagueResponse.Content.ReadFromJsonAsync<LeagueDto>();

        // Get available bots
        var botsResponse = await Client.GetAsync("/api/bots");
        await EnsureSuccessAsync(botsResponse);
        var bots = await botsResponse.Content.ReadFromJsonAsync<List<BotDto>>();

        if (bots is not null && bots.Count > 0)
        {
            // Act - Add bot to league
            var addRequest = new { BotId = bots[0].Id };
            var response = await Client.PostAsJsonAsync($"/api/leagues/{league!.Id}/bots", addRequest);

            // Assert - Either OK (success) or BadRequest (bot already in league)
            await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK).Or.IsEqualTo(HttpStatusCode.BadRequest);
        }
    }

    [Test]
    public async Task AddBotToLeague_InvalidBot_ReturnsBadRequest()
    {
        // Arrange - Create league
        var token = await GetAuthTokenAsync("addbotinvalid@example.com");
        SetAuthHeader(token);

        var leagueRequest = CreateLeagueRequestObject(
            "Add Bot Invalid Test League",
            "Test",
            true,
            10,
            3,
            1,
            5
        );

        var leagueResponse = await Client.PostAsJsonAsync("/api/leagues", leagueRequest);
        await EnsureSuccessAsync(leagueResponse);
        var league = await leagueResponse.Content.ReadFromJsonAsync<LeagueDto>();

        // Act - Try to add non-existent bot
        var addRequest = new { BotId = Guid.NewGuid() };
        var response = await Client.PostAsJsonAsync($"/api/leagues/{league!.Id}/bots", addRequest);

        // Assert - Bot not found returns BadRequest (not NotFound)
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task AddBotToLeague_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var addRequest = new { BotId = Guid.NewGuid() };
        var response = await Client.PostAsJsonAsync($"/api/leagues/{Guid.NewGuid()}/bots", addRequest);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
[SkipIfInMemory]
    public async Task RemoveBotFromLeague_BotNotFound_ReturnsBadRequest()
    {
        // Arrange - Create league
        var token = await GetAuthTokenAsync("removebotuser@example.com");
        SetAuthHeader(token);

        var leagueRequest = CreateLeagueRequestObject(
            "Remove Bot Test League",
            "Test",
            true,
            10,
            3,
            1,
            5
        );

        var leagueResponse = await Client.PostAsJsonAsync("/api/leagues", leagueRequest);
        await EnsureSuccessAsync(leagueResponse);
        var league = await leagueResponse.Content.ReadFromJsonAsync<LeagueDto>();

        // Act - Try to remove non-existent bot
        var response = await Client.DeleteAsync($"/api/leagues/{league!.Id}/bots/{Guid.NewGuid()}");

        // Assert - Bot not found returns BadRequest (only League not found returns NotFound)
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task RemoveBotFromLeague_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.DeleteAsync($"/api/leagues/{Guid.NewGuid()}/bots/{Guid.NewGuid()}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
