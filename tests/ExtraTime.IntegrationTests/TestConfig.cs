using ExtraTime.Infrastructure.Data;
using ExtraTime.IntegrationTests.Factories;
using ExtraTime.IntegrationTests.Fixtures;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Respawn;
using Respawn.Graph;

namespace ExtraTime.IntegrationTests;

public static class TestConfig
{
    public static SharedContainerFixture? SqlContainer { get; private set; }
    public static Respawner? Respawner { get; private set; }
    public static string? SharedConnectionString { get; private set; }

    // Default to InMemory if not specified
    public static bool UseSqlServer => Environment.GetEnvironmentVariable("TEST_MODE") == "SqlServer";

    [Before(Assembly)]
    public static async Task InitializeAsync()
    {
        if (UseSqlServer)
        {
            SqlContainer = new SharedContainerFixture();
            await SqlContainer.InitializeAsync();

            var masterConnString = SqlContainer.ConnectionString;
            var dbName = $"IntegrationTests_{Guid.NewGuid():N}";

            // 1. Create Shared Database
            // We use a unique name per run to avoid conflicts with previous runs (container reuse)
            await using (var conn = new SqlConnection(masterConnString))
            {
                await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"CREATE DATABASE [{dbName}]";
                await cmd.ExecuteNonQueryAsync();
            }

            var builder = new SqlConnectionStringBuilder(masterConnString)
            {
                InitialCatalog = dbName,
                // Ensure MultipleActiveResultSets is true if needed
                MultipleActiveResultSets = true 
            };
            SharedConnectionString = builder.ConnectionString;

            // 2. Initialize Schema (Once)
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(SharedConnectionString)
                .Options;

            await using (var context = new ApplicationDbContext(options))
            {
                await context.Database.EnsureCreatedAsync();
            }

            // 3. Initialize Respawner
            await using (var conn = new SqlConnection(SharedConnectionString))
            {
                await conn.OpenAsync();
                Respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
                {
                    DbAdapter = DbAdapter.SqlServer,
                    SchemasToInclude = new[] { "dbo" },
                    TablesToIgnore = new Table[] { "__EFMigrationsHistory" }
                });
            }
        }
    }

    [After(Assembly)]
    public static async Task CleanupAsync()
    {
        // 1. Drop the unique database created for this run
        if (UseSqlServer && SqlContainer != null && SharedConnectionString != null)
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(SharedConnectionString);
                var dbName = builder.InitialCatalog;
                var masterConnString = SqlContainer.ConnectionString;

                await using var conn = new SqlConnection(masterConnString);
                await conn.OpenAsync();
                SqlConnection.ClearAllPools();
                
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $@"
                    IF EXISTS (SELECT * FROM sys.databases WHERE name = '{dbName}')
                    BEGIN
                        ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                        DROP DATABASE [{dbName}];
                    END";
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to drop database: {ex.Message}");
            }
        }

        // 2. Dispose container (might stop it unless reuse is enabled/working)
        if (SqlContainer != null)
        {
            await SqlContainer.DisposeAsync();
        }
    }

    public static ITestDatabase CreateDatabase()
    {
        if (UseSqlServer && SharedConnectionString != null && Respawner != null)
        {
            return new SqlServerTestDatabase(SharedConnectionString, Respawner);
        }

        return new InMemoryTestDatabase();
    }
}

