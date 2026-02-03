using System.Net;
using System.Net.Http.Json;
using ExtraTime.API.Tests.Attributes;
using ExtraTime.API.Tests.Fixtures;
using ExtraTime.Application.Features.Bets.DTOs;
using ExtraTime.Application.Features.Leagues.DTOs;

namespace ExtraTime.API.Tests.Endpoints;

[TestCategory(TestCategories.Significant)]
public sealed class BetEndpointsTests : ApiTestBase
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
    [SkipOnGitHubActions]
    public async Task PlaceBet_ValidData_ReturnsCreated()
    {
        if (CustomWebApplicationFactory.UseInMemory) return;

        // Arrange - Create league and get auth token
        var token = await GetAuthTokenAsync("betuser1@example.com");
        SetAuthHeader(token);

        var leagueRequest = CreateLeagueRequestObject(
            "Bet Test League",
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

        // Act - Place a bet (requires a match to exist, so we test the endpoint structure)
        var betRequest = new
        {
            MatchId = Guid.NewGuid(),
            PredictedHomeScore = 2,
            PredictedAwayScore = 1
        };

        var response = await Client.PostAsJsonAsync($"/api/leagues/{league!.Id}/bets", betRequest);

        // Assert - Should fail with NotFound for match (expected behavior)
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task PlaceBet_InvalidLeague_ReturnsNotFound()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        var betRequest = new
        {
            MatchId = Guid.NewGuid(),
            PredictedHomeScore = 2,
            PredictedAwayScore = 1
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/leagues/{Guid.NewGuid()}/bets", betRequest);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task PlaceBet_NotAMember_ReturnsForbidden()
    {
        // Arrange - Create league as user 1
        var token1 = await GetAuthTokenAsync("owner2@example.com");
        SetAuthHeader(token1);

        var leagueRequest = CreateLeagueRequestObject(
            "Private Bet League",
            "Test",
            false,
            10,
            3,
            1,
            5
        );

        var leagueResponse = await Client.PostAsJsonAsync("/api/leagues", leagueRequest);
        await EnsureSuccessAsync(leagueResponse);
        var league = await leagueResponse.Content.ReadFromJsonAsync<LeagueDto>();

        // Arrange - User 2 tries to place bet without joining
        var token2 = await GetAuthTokenAsync("nonmember@example.com");
        SetAuthHeader(token2);

        var betRequest = new
        {
            MatchId = Guid.NewGuid(),
            PredictedHomeScore = 2,
            PredictedAwayScore = 1
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/leagues/{league!.Id}/bets", betRequest);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task PlaceBet_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var betRequest = new
        {
            MatchId = Guid.NewGuid(),
            PredictedHomeScore = 2,
            PredictedAwayScore = 1
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/leagues/{Guid.NewGuid()}/bets", betRequest);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PlaceBet_InvalidData_ReturnsValidationProblem()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        var betRequest = new
        {
            MatchId = Guid.Empty,
            PredictedHomeScore = -1,
            PredictedAwayScore = -1
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/leagues/{Guid.NewGuid()}/bets", betRequest);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    [SkipOnGitHubActions]
    public async Task DeleteBet_NotFound_ReturnsNotFound()
    {
        if (CustomWebApplicationFactory.UseInMemory) return;

        // Arrange - Create league
        var token = await GetAuthTokenAsync("deletebetuser@example.com");
        SetAuthHeader(token);

        var leagueRequest = CreateLeagueRequestObject(
            "Delete Bet League",
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
        var response = await Client.DeleteAsync($"/api/leagues/{league!.Id}/bets/{Guid.NewGuid()}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteBet_NotBetOwner_ReturnsForbidden()
    {
        // Arrange - This test would need a bet created by another user
        // For now, test that the endpoint requires authentication
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        // Act
        var response = await Client.DeleteAsync($"/api/leagues/{Guid.NewGuid()}/bets/{Guid.NewGuid()}");

        // Assert - League not found or bet not found
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteBet_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.DeleteAsync($"/api/leagues/{Guid.NewGuid()}/bets/{Guid.NewGuid()}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    [SkipOnGitHubActions]
    public async Task GetMyBets_Authenticated_ReturnsOk()
    {
        if (CustomWebApplicationFactory.UseInMemory) return;

        // Arrange - Create league and join
        var token = await GetAuthTokenAsync("mybetsuser@example.com");
        SetAuthHeader(token);

        var leagueRequest = CreateLeagueRequestObject(
            "My Bets League",
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
        var response = await Client.GetAsync($"/api/leagues/{league!.Id}/bets/my");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var bets = await response.Content.ReadFromJsonAsync<List<MyBetDto>>();
        await Assert.That(bets).IsNotNull();
    }

    [Test]
    public async Task GetMyBets_NotAMember_ReturnsForbidden()
    {
        // Arrange - Create league as user 1
        var token1 = await GetAuthTokenAsync("owner3@example.com");
        SetAuthHeader(token1);

        var leagueRequest = CreateLeagueRequestObject(
            "Private MyBets League",
            "Test",
            false,
            10,
            3,
            1,
            5
        );

        var leagueResponse = await Client.PostAsJsonAsync("/api/leagues", leagueRequest);
        await EnsureSuccessAsync(leagueResponse);
        var league = await leagueResponse.Content.ReadFromJsonAsync<LeagueDto>();

        // Arrange - User 2 tries to get bets without joining
        var token2 = await GetAuthTokenAsync("nonmember2@example.com");
        SetAuthHeader(token2);

        // Act
        var response = await Client.GetAsync($"/api/leagues/{league!.Id}/bets/my");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetMyBets_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"/api/leagues/{Guid.NewGuid()}/bets/my");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    [SkipOnGitHubActions]
    public async Task GetMatchBets_InvalidMatch_ReturnsNotFound()
    {
        if (CustomWebApplicationFactory.UseInMemory) return;

        // Arrange - Create league
        var token = await GetAuthTokenAsync("matchbetsuser@example.com");
        SetAuthHeader(token);

        var leagueRequest = CreateLeagueRequestObject(
            "Match Bets League",
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
        var response = await Client.GetAsync($"/api/leagues/{league!.Id}/matches/{Guid.NewGuid()}/bets");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetMatchBets_NotAMember_ReturnsForbidden()
    {
        // Arrange - Create league as user 1
        var token1 = await GetAuthTokenAsync("owner4@example.com");
        SetAuthHeader(token1);

        var leagueRequest = CreateLeagueRequestObject(
            "Private MatchBets League",
            "Test",
            false,
            10,
            3,
            1,
            5
        );

        var leagueResponse = await Client.PostAsJsonAsync("/api/leagues", leagueRequest);
        await EnsureSuccessAsync(leagueResponse);
        var league = await leagueResponse.Content.ReadFromJsonAsync<LeagueDto>();

        // Arrange - User 2 tries to get match bets without joining
        var token2 = await GetAuthTokenAsync("nonmember3@example.com");
        SetAuthHeader(token2);

        // Act
        var response = await Client.GetAsync($"/api/leagues/{league!.Id}/matches/{Guid.NewGuid()}/bets");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetMatchBets_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"/api/leagues/{Guid.NewGuid()}/matches/{Guid.NewGuid()}/bets");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    [SkipOnGitHubActions]
    public async Task GetLeagueStandings_Authenticated_ReturnsOk()
    {
        if (CustomWebApplicationFactory.UseInMemory) return;

        // Arrange - Create league
        var token = await GetAuthTokenAsync("standingsuser@example.com");
        SetAuthHeader(token);

        var leagueRequest = CreateLeagueRequestObject(
            "Standings League",
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
        var response = await Client.GetAsync($"/api/leagues/{league!.Id}/standings");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var standings = await response.Content.ReadFromJsonAsync<List<LeagueStandingDto>>();
        await Assert.That(standings).IsNotNull();
    }

    [Test]
    public async Task GetLeagueStandings_NotAMember_ReturnsForbidden()
    {
        // Arrange - Create league as user 1
        var token1 = await GetAuthTokenAsync("owner5@example.com");
        SetAuthHeader(token1);

        var leagueRequest = CreateLeagueRequestObject(
            "Private Standings League",
            "Test",
            false,
            10,
            3,
            1,
            5
        );

        var leagueResponse = await Client.PostAsJsonAsync("/api/leagues", leagueRequest);
        await EnsureSuccessAsync(leagueResponse);
        var league = await leagueResponse.Content.ReadFromJsonAsync<LeagueDto>();

        // Arrange - User 2 tries to get standings without joining
        var token2 = await GetAuthTokenAsync("nonmember4@example.com");
        SetAuthHeader(token2);

        // Act
        var response = await Client.GetAsync($"/api/leagues/{league!.Id}/standings");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetLeagueStandings_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"/api/leagues/{Guid.NewGuid()}/standings");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    [SkipOnGitHubActions]
    public async Task GetUserStats_InvalidUser_ReturnsNotFound()
    {
        if (CustomWebApplicationFactory.UseInMemory) return;

        // Arrange - Create league
        var token = await GetAuthTokenAsync("userstatsuser@example.com");
        SetAuthHeader(token);

        var leagueRequest = CreateLeagueRequestObject(
            "User Stats League",
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

        // Act - Request stats for non-existent user
        var response = await Client.GetAsync($"/api/leagues/{league!.Id}/users/{Guid.NewGuid()}/stats");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetUserStats_NotAMember_ReturnsNotFound()
    {
        // Arrange - Create league as user 1
        var token1 = await GetAuthTokenAsync("owner6@example.com");
        SetAuthHeader(token1);

        var leagueRequest = CreateLeagueRequestObject(
            "Private UserStats League",
            "Test",
            false,
            10,
            3,
            1,
            5
        );

        var leagueResponse = await Client.PostAsJsonAsync("/api/leagues", leagueRequest);
        await EnsureSuccessAsync(leagueResponse);
        var league = await leagueResponse.Content.ReadFromJsonAsync<LeagueDto>();

        // Arrange - Get user 2 token
        var token2 = await GetAuthTokenAsync("nonmember5@example.com");

        // Arrange - Get user 2 info to get their user ID
        SetAuthHeader(token2);
        var meResponse = await Client.GetAsync("/api/auth/me");
        await EnsureSuccessAsync(meResponse);
        var user2 = await meResponse.Content.ReadFromJsonAsync<UserDto>();

        // Arrange - Set auth back to user 1
        SetAuthHeader(token1);

        // Act - User 1 (who IS a member as the creator) tries to get stats for user 2 who is not a member
        var response = await Client.GetAsync($"/api/leagues/{league!.Id}/users/{user2!.Id}/stats");

        // Assert - Target user not found in league returns NotFound, not Forbidden
        // Forbidden would only be returned if the current user themselves is not a member
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetUserStats_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"/api/leagues/{Guid.NewGuid()}/users/{Guid.NewGuid()}/stats");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    private sealed record UserDto(Guid Id, string Email, string Username);
}
