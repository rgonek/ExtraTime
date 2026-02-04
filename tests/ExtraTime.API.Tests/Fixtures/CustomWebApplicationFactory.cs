using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Respawn;
using Testcontainers.MsSql;

namespace ExtraTime.API.Tests.Fixtures;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private static MsSqlContainer? Container;
    public static Respawner? Respawner { get; private set; }

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

            // Check for environment variable configuration
            var dbType = Environment.GetEnvironmentVariable("TEST_MODE");
            if (!string.Equals(dbType, "SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                _useInMemory = true;
                Environment.SetEnvironmentVariable("UseInMemoryDatabase", "true");
            }
            else
            {
                Container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
                    .Build();

                await Container.StartAsync();
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

                // Initialize Respawner ONCE after migration
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();
                Respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
                {
                    DbAdapter = DbAdapter.SqlServer,
                    SchemasToInclude = new[] { "dbo" }
                });
                await connection.CloseAsync();
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
                var connectionString = Container.GetConnectionString();
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    // Standard connection string for non-Aspire scenarios
                    { "ConnectionStrings:DefaultConnection", connectionString },
                    // Aspire uses this connection string name
                    { "ConnectionStrings:extratime", connectionString }
                });
            }
        });

        builder.ConfigureTestServices(services =>
        {
            // Remove ALL DbContext-related services to prevent conflicts with Aspire pooling
            var dbContextDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("ApplicationDbContext") == true ||
                            d.ServiceType.FullName?.Contains("DbContextPool") == true ||
                            d.ServiceType.FullName?.Contains("DbContextLease") == true ||
                            d.ImplementationType?.FullName?.Contains("ApplicationDbContext") == true)
                .ToList();

            foreach (var descriptor in dbContextDescriptors)
            {
                services.Remove(descriptor);
            }

            // Also remove the generic DbContextOptions
            services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
            services.RemoveAll(typeof(DbContextOptions));

            // Also remove IApplicationDbContext registration
            services.RemoveAll(typeof(IApplicationDbContext));

            // Re-register DbContext with test configuration (without pooling)
            if (_useInMemory)
            {
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb")
                           .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)));
            }
            else if (Container != null)
            {
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(Container.GetConnectionString()));
            }

            // Re-register IApplicationDbContext
            services.AddScoped<IApplicationDbContext>(provider =>
                provider.GetRequiredService<ApplicationDbContext>());

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
