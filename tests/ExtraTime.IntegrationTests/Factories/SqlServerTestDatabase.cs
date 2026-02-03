using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Infrastructure.Data;
using Mediator;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.IntegrationTests.Factories;

public class SqlServerTestDatabase : ITestDatabase
{
    private readonly string _masterConnectionString;
    private readonly string _dbName;
    private readonly string _connectionString;
    private ApplicationDbContext? _context;

    public SqlServerTestDatabase(string masterConnectionString)
    {
        _masterConnectionString = masterConnectionString;
        _dbName = $"DB_{Guid.NewGuid():N}"; // valid sql identifier

        var builder = new SqlConnectionStringBuilder(_masterConnectionString)
        {
            InitialCatalog = _dbName
        };
        _connectionString = builder.ConnectionString;
    }

    public string ConnectionString => _connectionString;

    public async Task InitializeAsync()
    {
        // 1. Create Database
        await using (var conn = new SqlConnection(_masterConnectionString))
        {
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandTimeout = 60; // Increase timeout for heavy parallel load
            cmd.CommandText = $"CREATE DATABASE [{_dbName}]";
            await cmd.ExecuteNonQueryAsync();
        }

        // 2. Initialize Schema (EnsureCreated is faster than Migrate for tests usually)
        _context = CreateContext();
        _context.Database.SetCommandTimeout(120); // Schema creation can be slow under load
        await _context.Database.EnsureCreatedAsync();
    }

    public ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        return new ApplicationDbContext(
            options,
            Substitute.For<ICurrentUserService>(), // Can be overridden in test
            Substitute.For<IMediator>());
    }

    public async ValueTask DisposeAsync()
    {
        if (_context != null)
        {
            await _context.DisposeAsync();
        }

        // Drop Database to clean up
        // Note: Use master connection to drop
        try
        {
            SqlConnection.ClearAllPools(); // Important to release locks
            await using var conn = new SqlConnection(_masterConnectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            // Force drop by setting single user
            cmd.CommandText = $@"
                IF EXISTS (SELECT * FROM sys.databases WHERE name = '{_dbName}')
                BEGIN
                    ALTER DATABASE [{_dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{_dbName}];
                END";
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to drop database {_dbName}: {ex.Message}");
        }
    }
}
