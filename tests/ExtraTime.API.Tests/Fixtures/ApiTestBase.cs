using System.Data.Common;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ExtraTime.API.Tests.Fixtures;
using ExtraTime.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Respawn;

namespace ExtraTime.API.Tests.Fixtures;

public abstract class ApiTestBase
{
    private static readonly CustomWebApplicationFactory Factory = new();
    private Respawner _respawner = null!;
    protected HttpClient Client { get; private set; } = null!;

    [Before(Test)]
    public async Task InitializeAsync()
    {
        await Factory.EnsureInitializedAsync();
        Client = Factory.CreateClient();

        // Initialize Respawn for database cleanup
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"]
        });

        await _respawner.ResetAsync(connection);
    }

    [After(Test)]
    public void DisposeTest()
    {
        Client.Dispose();
    }

    protected async Task<string> GetAuthTokenAsync(string email = "test@example.com", string password = "Test123!")
    {
        var registerResponse = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Username = email.Split('@')[0],
            Password = password
        });

        if (!registerResponse.IsSuccessStatusCode)
        {
            var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new
            {
                Email = email,
                Password = password
            });

            var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
            return loginResult!.AccessToken;
        }

        var registerResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        return registerResult!.AccessToken;
    }

    protected void SetAuthHeader(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private sealed record AuthResponse(string AccessToken, string RefreshToken);
}
