using System.Net;
using System.Net.Http.Json;
using ExtraTime.API.Tests.Attributes;
using ExtraTime.API.Tests.Fixtures;
using ExtraTime.Application.Features.Admin.DTOs;

namespace ExtraTime.API.Tests.Endpoints;

[TestCategory(TestCategories.Significant)]
public sealed class AdminEndpointsTests : ApiTestBase
{
    [Test]
    public async Task GetJobs_NonAdminUser_ReturnsForbidden()
    {
        // Arrange - Regular user token
        var token = await GetAuthTokenAsync("regularuser@example.com");
        SetAuthHeader(token);

        // Act
        var response = await Client.GetAsync("/api/admin/jobs");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetJobs_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/admin/jobs");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetJobsStats_NonAdminUser_ReturnsForbidden()
    {
        // Arrange - Regular user token
        var token = await GetAuthTokenAsync("regularuser2@example.com");
        SetAuthHeader(token);

        // Act
        var response = await Client.GetAsync("/api/admin/jobs/stats");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetJobsStats_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/admin/jobs/stats");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetJobById_NonAdminUser_ReturnsForbidden()
    {
        // Arrange - Regular user token
        var token = await GetAuthTokenAsync("regularuser3@example.com");
        SetAuthHeader(token);

        // Act
        var response = await Client.GetAsync($"/api/admin/jobs/{Guid.NewGuid()}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetJobById_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"/api/admin/jobs/{Guid.NewGuid()}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task RetryJob_NonAdminUser_ReturnsForbidden()
    {
        // Arrange - Regular user token
        var token = await GetAuthTokenAsync("regularuser4@example.com");
        SetAuthHeader(token);

        // Act
        var response = await Client.PostAsJsonAsync($"/api/admin/jobs/{Guid.NewGuid()}/retry", new { });

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task RetryJob_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.PostAsJsonAsync($"/api/admin/jobs/{Guid.NewGuid()}/retry", new { });

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CancelJob_NonAdminUser_ReturnsForbidden()
    {
        // Arrange - Regular user token
        var token = await GetAuthTokenAsync("regularuser5@example.com");
        SetAuthHeader(token);

        // Act
        var response = await Client.PostAsJsonAsync($"/api/admin/jobs/{Guid.NewGuid()}/cancel", new { });

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task CancelJob_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.PostAsJsonAsync($"/api/admin/jobs/{Guid.NewGuid()}/cancel", new { });

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    [SkipOnGitHubActions]
    public async Task GetJobs_WithPagination_ReturnsOkOrForbidden()
    {
        // Arrange - Regular user token (admin endpoints require Admin role)
        var token = await GetAuthTokenAsync("paginationuser@example.com");
        SetAuthHeader(token);

        // Act
        var response = await Client.GetAsync("/api/admin/jobs?page=1&pageSize=10");

        // Assert - Should be forbidden for regular users
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetJobs_WithStatusFilter_NonAdmin_ReturnsForbidden()
    {
        // Arrange - Regular user token
        var token = await GetAuthTokenAsync("statusfilteruser@example.com");
        SetAuthHeader(token);

        // Act
        var response = await Client.GetAsync("/api/admin/jobs?status=Pending");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }
}
