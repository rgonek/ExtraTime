using ExtraTime.Infrastructure.Data;
using ExtraTime.IntegrationTests.Attributes;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.IntegrationTests.Tests.Infrastructure;

public sealed class MigrationTests
{
    [Test]
    [SkipIfInMemory]
    public async Task AllMigrations_ShouldApplySuccessfully()
    {
        // Arrange - Create a fresh database
        var masterConnectionString = TestConfig.SqlContainer!.ConnectionString;
        var dbName = $"MigrationTest_{Guid.NewGuid():N}";

        await using (var conn = new SqlConnection(masterConnectionString))
        {
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"CREATE DATABASE [{dbName}]";
            await cmd.ExecuteNonQueryAsync();
        }

        var builder = new SqlConnectionStringBuilder(masterConnectionString)
        {
            InitialCatalog = dbName,
            MultipleActiveResultSets = true
        };
        var connectionString = builder.ConnectionString;

        try
        {
            // Act - Run all migrations
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            await using var context = new ApplicationDbContext(options);
            await context.Database.MigrateAsync();

            // Assert - Verify migrations table exists and has records
            var migrations = await context.Database
                .SqlQuery<string>($"SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId")
                .ToListAsync();

            await Assert.That(migrations).IsNotEmpty();
            await Assert.That(migrations.Count).IsGreaterThan(0);

            // Verify key tables were created
            var tables = await context.Database
                .SqlQuery<string>($"SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'")
                .ToListAsync();

            await Assert.That(tables).Contains("__EFMigrationsHistory");
            await Assert.That(tables).Contains("Competitions");
            await Assert.That(tables).Contains("Teams");
            await Assert.That(tables).Contains("Matches");
            await Assert.That(tables).Contains("Seasons");
            await Assert.That(tables).Contains("SeasonTeams");
            await Assert.That(tables).Contains("FootballStandings");
        }
        finally
        {
            // Cleanup - Drop the test database
            await using var conn = new SqlConnection(masterConnectionString);
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
    }
}
