using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Testcontainers.PostgreSql;

namespace ExtraTime.API.Tests.Fixtures;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private static PostgreSqlContainer? Container;

    private static bool _initialized;
    private static bool _migrated;
    private static bool _useInMemory;
    private static readonly SemaphoreSlim InitLock = new(1, 1);

    public static bool UseInMemory => _useInMemory;

    public async Task EnsureInitializedAsync()
    {
        if (_initialized)
            return;

        await InitLock.WaitAsync();
        try
        {
            if (_initialized)
                return;

            try
            {
                // Check for environment variable configuration
                var dbType = Environment.GetEnvironmentVariable("TEST_DATABASE_TYPE");
                if (string.Equals(dbType, "InMemory", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception("Forced InMemory");
                }

                Container = new PostgreSqlBuilder("postgres:17-alpine")
                    .WithDatabase("extratime_api_test")
                    .WithUsername("postgres")
                    .WithPassword("postgres")
                    .Build();

                await Container.StartAsync();
            }
            catch
            {
                _useInMemory = true;
                Environment.SetEnvironmentVariable("UseInMemoryDatabase", "true");
            }
            _initialized = true;
        }
        finally
        {
            InitLock.Release();
        }
    }

    public async Task EnsureMigratedAsync()
    {
        if (_migrated)
            return;

        await InitLock.WaitAsync();
        try
        {
            if (_migrated)
                return;

            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            if (_useInMemory)
            {
                await context.Database.EnsureCreatedAsync();
            }
            else
            {
                await context.Database.MigrateAsync();
            }
            _migrated = true;
        }
        finally
        {
            InitLock.Release();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            if (_useInMemory)
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "UseInMemoryDatabase", "true" }
                });
            }
            else if (Container != null)
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ConnectionStrings:DefaultConnection", Container.GetConnectionString() }
                });
            }
        });

        builder.ConfigureTestServices(services =>
        {
            // Mock external services
            var mockFootballDataService = Substitute.For<IFootballDataService>();
            services.RemoveAll(typeof(IFootballDataService));
            services.AddSingleton(mockFootballDataService);

            // Remove background service
            services.RemoveAll(typeof(IHostedService));
        });
    }

    public string GetConnectionString() => _useInMemory || Container == null ? string.Empty : Container.GetConnectionString();

    public static async Task DisposeContainerAsync()
    {
        if (!_useInMemory && Container != null)
            await Container.DisposeAsync();
    }
}
