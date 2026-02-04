using System.Data.Common;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ExtraTime.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Respawn;

namespace ExtraTime.API.Tests.Fixtures;

public abstract class ApiTestBase
{
    private static readonly CustomWebApplicationFactory Factory = new();
    private static readonly SemaphoreSlim DatabaseLock = new(1, 1);
    private bool _lockTaken;
    protected HttpClient Client { get; private set; } = null!;

    [Before(Test)]

    public async Task InitializeAsync()
    {
        await DatabaseLock.WaitAsync();
        _lockTaken = true;

        try
        {
            await Factory.EnsureInitializedAsync();
            Client = Factory.CreateClient();
            await Factory.EnsureMigratedAsync();

            // Initialize Respawn for database cleanup or handle InMemory
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (CustomWebApplicationFactory.UseInMemory)
            {
                await context.Database.EnsureDeletedAsync();
                await context.Database.EnsureCreatedAsync();
            }
            else
            {
                var connection = context.Database.GetDbConnection();

                if (connection.State != System.Data.ConnectionState.Open)
                    await connection.OpenAsync();

                if (CustomWebApplicationFactory.Respawner != null)
                {
                    await CustomWebApplicationFactory.Respawner.ResetAsync(connection);
                }
            }
        }
        catch (Exception)

        {
            if (_lockTaken)
            {
                DatabaseLock.Release();
                _lockTaken = false;
            }
            throw;
        }
    }

    [After(Test)]
    public void DisposeTest()
    {
        Client?.Dispose();

        if (_lockTaken)
        {
            DatabaseLock.Release();
            _lockTaken = false;
        }
    }

    protected async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new Exception($"Response failed with {response.StatusCode}: {content}");
        }
    }

    protected async Task<string> GetAuthTokenAsync(string email = "testuser@example.com", string password = "Test123!")
    {
        var registerRequest = new
        {
            Email = email,
            Username = email.Split('@')[0].Length < 3 ? email.Split('@')[0] + "123" : email.Split('@')[0],
            Password = password
        };

        var registerResponse = await Client.PostAsJsonAsync("/api/auth/register", registerRequest);

        if (registerResponse.IsSuccessStatusCode)
        {
            var registerResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
            return registerResult!.AccessToken;
        }

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password = password
        });

        if (!loginResponse.IsSuccessStatusCode)
        {
            var error = await loginResponse.Content.ReadAsStringAsync();
            var regError = await registerResponse.Content.ReadAsStringAsync();
            throw new Exception($"Failed to get auth token. \nRegister Status: {registerResponse.StatusCode}, Error: {regError}\nLogin Status: {loginResponse.StatusCode}, Error: {error}");
        }

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        return loginResult!.AccessToken;
    }

    protected void SetAuthHeader(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private sealed record AuthResponse(string AccessToken, string RefreshToken);
}
