using System.Net;
using System.Net.Http.Json;
using ExtraTime.API.Tests.Attributes;
using ExtraTime.API.Tests.Fixtures;
using ExtraTime.Application.Features.Football.DTOs;

namespace ExtraTime.API.Tests.Endpoints;

[TestCategory(TestCategories.Significant)]
public sealed class FootballEndpointsTests : ApiTestBase
{
    [Test]
    public async Task GetCompetitions_Anonymous_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/api/competitions");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var competitions = await response.Content.ReadFromJsonAsync<IReadOnlyList<CompetitionDto>>();
        await Assert.That(competitions).IsNotNull();
    }

    [Test]
    public async Task GetCompetitions_Authenticated_ReturnsOk()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        // Act
        var response = await Client.GetAsync("/api/competitions");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var competitions = await response.Content.ReadFromJsonAsync<IReadOnlyList<CompetitionDto>>();
        await Assert.That(competitions).IsNotNull();
    }

    [Test]
    public async Task GetMatches_Anonymous_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/api/matches");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetMatches_WithPagination_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/api/matches?page=1&pageSize=10");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetMatches_WithCompetitionFilter_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync($"/api/matches?competitionId={Guid.NewGuid()}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetMatches_WithDateRange_ReturnsOk()
    {
        // Arrange
        var dateFrom = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd");
        var dateTo = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd");

        // Act
        var response = await Client.GetAsync($"/api/matches?dateFrom={dateFrom}&dateTo={dateTo}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetMatches_WithStatusFilter_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/api/matches?status=Scheduled");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetMatchById_InvalidId_ReturnsNotFound()
    {
        // Act
        var response = await Client.GetAsync($"/api/matches/{Guid.NewGuid()}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetMatchById_Anonymous_ReturnsNotFoundForInvalidId()
    {
        // Act
        var response = await Client.GetAsync($"/api/matches/{Guid.NewGuid()}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetMatchById_Authenticated_ReturnsNotFoundForInvalidId()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        // Act
        var response = await Client.GetAsync($"/api/matches/{Guid.NewGuid()}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }
}
