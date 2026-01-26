using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Testcontainers.PostgreSql;

namespace ExtraTime.API.Tests.Fixtures;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly PostgreSqlContainer Container = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("extratime_api_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private static bool _initialized;
    private static readonly SemaphoreSlim InitLock = new(1, 1);

    public async Task EnsureInitializedAsync()
    {
        if (_initialized)
            return;

        await InitLock.WaitAsync();
        try
        {
            if (_initialized)
                return;

            await Container.StartAsync();
            _initialized = true;
        }
        finally
        {
            InitLock.Release();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove existing DbContext registration
            services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));

            // Add test database
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(Container.GetConnectionString()));

            // Mock external services
            var mockFootballDataService = Substitute.For<IFootballDataService>();
            services.RemoveAll(typeof(IFootballDataService));
            services.AddSingleton(mockFootballDataService);

            // Ensure database is created
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Database.Migrate();
        });
    }

    public string GetConnectionString() => Container.GetConnectionString();

    public static async Task DisposeContainerAsync()
    {
        await Container.DisposeAsync();
    }
}
