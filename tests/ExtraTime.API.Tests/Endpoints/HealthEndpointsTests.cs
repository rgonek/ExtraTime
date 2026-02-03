using System.Net;
using System.Net.Http.Json;
using ExtraTime.API.Tests.Attributes;
using ExtraTime.API.Tests.Fixtures;

namespace ExtraTime.API.Tests.Endpoints;

[TestCategory(TestCategories.Significant)]
public sealed class HealthEndpointsTests : ApiTestBase
{
    private sealed record HealthResponse(string Status, DateTime Timestamp, string Version);

    [Test]
    public async Task GetHealth_Anonymous_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/api/health");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var health = await response.Content.ReadFromJsonAsync<HealthResponse>();
        await Assert.That(health).IsNotNull();
        await Assert.That(health!.Status).IsEqualTo("Healthy");
        await Assert.That(health.Version).IsEqualTo("1.0.0");
        await Assert.That(health.Timestamp).IsNotEqualTo(default(DateTime));
    }

    [Test]
    public async Task GetHealth_Authenticated_ReturnsOk()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        SetAuthHeader(token);

        // Act
        var response = await Client.GetAsync("/api/health");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var health = await response.Content.ReadFromJsonAsync<HealthResponse>();
        await Assert.That(health).IsNotNull();
        await Assert.That(health!.Status).IsEqualTo("Healthy");
    }
}
