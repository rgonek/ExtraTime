using System.Net;
using ExtraTime.API.Tests.Attributes;
using ExtraTime.API.Tests.Fixtures;

namespace ExtraTime.API.Tests.Endpoints;

[TestCategory(TestCategories.Significant)]
public sealed class HeadToHeadEndpointsTests : ApiTestBase
{
    [Test]
    public async Task GetHeadToHead_MissingQueryParams_ReturnsBadRequest()
    {
        // Act
        var response = await Client.GetAsync("/api/football/head-to-head");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetHeadToHead_SameTeamIds_ReturnsBadRequest()
    {
        // Arrange
        var teamId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/football/head-to-head?team1Id={teamId}&team2Id={teamId}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }
}
