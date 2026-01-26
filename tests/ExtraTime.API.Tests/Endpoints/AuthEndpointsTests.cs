using System.Net;
using System.Net.Http.Json;
using ExtraTime.API.Tests.Fixtures;

namespace ExtraTime.API.Tests.Endpoints;

public sealed class AuthEndpointsTests : ApiTestBase
{
    [Test]
    public async Task Register_ValidData_ReturnsOk()
    {
        // Arrange
        var request = new
        {
            Email = "newuser@example.com",
            Username = "newuser",
            Password = "Password123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        await Assert.That(result!.AccessToken).IsNotNull();
    }

    [Test]
    public async Task Login_ValidCredentials_ReturnsOk()
    {
        // Arrange
        await GetAuthTokenAsync("login@example.com", "Password123!");

        var request = new
        {
            Email = "login@example.com",
            Password = "Password123!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetCurrentUser_Authenticated_ReturnsUser()
    {
        // Arrange
        var token = await GetAuthTokenAsync("me@example.com", "Password123!");
        SetAuthHeader(token);

        // Act
        var response = await Client.GetAsync("/api/auth/me");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<UserDto>();
        await Assert.That(result!.Email).IsEqualTo("me@example.com");
    }

    private sealed record AuthResponseDto(string AccessToken, string RefreshToken);
    private sealed record UserDto(Guid Id, string Email, string Username, string Role);

    [Test]
    public async Task GetCurrentUser_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/auth/me");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
