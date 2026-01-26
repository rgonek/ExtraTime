using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace ExtraTime.IntegrationTests.Fixtures;

public sealed class DatabaseFixture
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("extratime_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private Respawner _respawner = null!;
    private NpgsqlConnection _connection = null!;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public string ConnectionString => _container.GetConnectionString();

    public async Task EnsureInitializedAsync()
    {
        if (_initialized)
            return;

        await _initLock.WaitAsync();
        try
        {
            if (_initialized)
                return;

            await _container.StartAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;

            var mockCurrentUserService = Substitute.For<ICurrentUserService>();
            await using var context = new ApplicationDbContext(options, mockCurrentUserService);
            await context.Database.MigrateAsync();

            _connection = new NpgsqlConnection(ConnectionString);
            await _connection.OpenAsync();

            _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = ["public"]
            });

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_connection);
    }

    public async Task DisposeAsync()
    {
        if (_connection != null)
            await _connection.DisposeAsync();
        await _container.DisposeAsync();
    }
}
