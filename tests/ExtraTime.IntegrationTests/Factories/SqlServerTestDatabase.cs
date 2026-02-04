using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Infrastructure.Data;
using Mediator;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Respawn;

namespace ExtraTime.IntegrationTests.Factories;

public class SqlServerTestDatabase : ITestDatabase
{
    private readonly string _connectionString;
    private readonly Respawner _respawner;
    private ApplicationDbContext? _context;

    public SqlServerTestDatabase(string connectionString, Respawner respawner)
    {
        _connectionString = connectionString;
        _respawner = respawner;
    }

    public string ConnectionString => _connectionString;

    public async Task InitializeAsync()
    {
        // Reset the shared database state
        await ResetAsync();
    }

    public async Task ResetAsync()
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        await _respawner.ResetAsync(conn);
    }

    public ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        _context = new ApplicationDbContext(
            options,
            Substitute.For<ICurrentUserService>(), // Can be overridden in test
            Substitute.For<IMediator>());

        return _context;
    }

    public async ValueTask DisposeAsync()
    {
        if (_context != null)
        {
            await _context.DisposeAsync();
        }
        // No DB drop needed as we use a shared database
    }
}
