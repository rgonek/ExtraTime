using System.Net;
using System.Net.Http.Json;
using ExtraTime.API.Tests.Attributes;
using ExtraTime.API.Tests.Fixtures;

namespace ExtraTime.API.Tests.Endpoints;

[TestCategory(TestCategories.Significant)]
public sealed class FootballSyncEndpointsTests : ApiTestBase
{
    [Test]
    public async Task SyncCompetitions_NonAdminUser_ReturnsForbidden()
    {
        // Arrange - Regular user token
        var token = await GetAuthTokenAsync("regularuser@example.com");
        SetAuthHeader(token);

        // Act
        var response = await Client.PostAsJsonAsync("/api/admin/sync/competitions", new { });

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task SyncCompetitions_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/admin/sync/competitions", new { });

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task SyncTeams_NonAdminUser_ReturnsForbidden()
    {
        // Arrange - Regular user token
        var token = await GetAuthTokenAsync("regularuser2@example.com");
        SetAuthHeader(token);

        // Act
        var response = await Client.PostAsJsonAsync($"/api/admin/sync/teams/{Guid.NewGuid()}", new { });

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task SyncTeams_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.PostAsJsonAsync($"/api/admin/sync/teams/{Guid.NewGuid()}", new { });

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task SyncMatches_NonAdminUser_ReturnsForbidden()
    {
        // Arrange - Regular user token
        var token = await GetAuthTokenAsync("regularuser3@example.com");
        SetAuthHeader(token);

        // Act
        var response = await Client.PostAsJsonAsync("/api/admin/sync/matches", new { });

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task SyncMatches_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/admin/sync/matches", new { });

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task SyncLiveMatches_NonAdminUser_ReturnsForbidden()
    {
        // Arrange - Regular user token
        var token = await GetAuthTokenAsync("regularuser4@example.com");
        SetAuthHeader(token);

        // Act
        var response = await Client.PostAsJsonAsync("/api/admin/sync/live", new { });

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task SyncLiveMatches_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/admin/sync/live", new { });

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task SyncMatches_WithDateRange_NonAdmin_ReturnsForbidden()
    {
        // Arrange - Regular user token
        var token = await GetAuthTokenAsync("regularuser5@example.com");
        SetAuthHeader(token);

        var dateFrom = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd");
        var dateTo = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd");

        // Act
        var response = await Client.PostAsJsonAsync($"/api/admin/sync/matches?dateFrom={dateFrom}&dateTo={dateTo}", new { });

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task SyncTeams_WithValidCompetition_NonAdmin_ReturnsForbidden()
    {
        // Arrange - Regular user token
        var token = await GetAuthTokenAsync("regularuser6@example.com");
        SetAuthHeader(token);

        // Act
        var response = await Client.PostAsJsonAsync($"/api/admin/sync/teams/{Guid.NewGuid()}", new { });

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }
}
