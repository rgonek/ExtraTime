using System.Net;
using System.Net.Http.Json;
using ExtraTime.API.Tests.Attributes;
using ExtraTime.API.Tests.Fixtures;

namespace ExtraTime.API.Tests.Endpoints;

[TestCategory(TestCategories.Significant)]
public sealed class AdminBotManagementEndpointsTests : ApiTestBase
{
    [Test]
    public async Task AdminBotsEndpoints_NonAdminUser_ReturnsForbidden()
    {
        var token = await GetAuthTokenAsync("adminbotsnonadmin@example.com");
        SetAuthHeader(token);

        var response = await Client.GetAsync("/api/admin/bots");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task AdminBotsEndpoints_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/admin/bots");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CreateAdminBot_NonAdminUser_ReturnsForbidden()
    {
        var token = await GetAuthTokenAsync("createadminbotnonadmin@example.com");
        SetAuthHeader(token);

        var request = new
        {
            Name = "Endpoint Test Bot",
            AvatarUrl = "https://example.com/bot.png",
            Strategy = "Random"
        };

        var response = await Client.PostAsJsonAsync("/api/admin/bots", request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task AdminIntegrationEndpoints_NonAdminUser_ReturnsForbidden()
    {
        var token = await GetAuthTokenAsync("integrationsnonadmin@example.com");
        SetAuthHeader(token);

        var response = await Client.GetAsync("/api/admin/integrations");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task AdminIntegrationEndpoints_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/admin/integrations");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task AdminExternalDataEndpoints_NonAdminUser_ReturnsForbidden()
    {
        var token = await GetAuthTokenAsync("externaldatanonadmin@example.com");
        SetAuthHeader(token);

        var response = await Client.PostAsJsonAsync("/api/admin/external-data/understat/sync", new { });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task AdminExternalDataEndpoints_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync("/api/admin/external-data/understat/sync", new { });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
