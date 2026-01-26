using System.Net;
using System.Net.Http.Json;
using ExtraTime.API.Tests.Fixtures;
using ExtraTime.Application.Features.Leagues.DTOs;

namespace ExtraTime.API.Tests.Endpoints;

public sealed class LeagueEndpointsTests : ApiTestBase
{
    [Test]
    public async Task CreateLeague_Authenticated_ReturnsOk()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        var request = new
        {
            Name = "API Test League",
            Description = "Test Description",
            IsPublic = true,
            MaxMembers = 20,
            ScoreExactMatch = 3,
            ScoreCorrectResult = 1,
            BettingDeadlineMinutes = 5
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/leagues", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
        
        var league = await response.Content.ReadFromJsonAsync<LeagueDto>();
        await Assert.That(league).IsNotNull();
        await Assert.That(league!.Name).IsEqualTo("API Test League");
        await Assert.That(league.InviteCode).IsNotNull();
    }

    [Test]
    public async Task JoinLeague_ValidCode_ReturnsOk()
    {
        // Arrange - Create league by user 1
        var token1 = await GetAuthTokenAsync("owner@example.com");
        SetAuthHeader(token1);
        
        var createResponse = await Client.PostAsJsonAsync("/api/leagues", new { 
            Name = "Joinable League", 
            MaxMembers = 10,
            ScoreExactMatch = 3,
            ScoreCorrectResult = 1,
            BettingDeadlineMinutes = 5
        });
        var league = await createResponse.Content.ReadFromJsonAsync<LeagueDto>();

        // Arrange - User 2 joins
        var token2 = await GetAuthTokenAsync("joiner@example.com");
        SetAuthHeader(token2);

        // Act
        var response = await Client.PostAsJsonAsync($"/api/leagues/{league!.Id}/join", new { InviteCode = league.InviteCode });

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}
