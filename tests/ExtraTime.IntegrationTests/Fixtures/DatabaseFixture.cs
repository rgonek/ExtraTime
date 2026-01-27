using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Infrastructure.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace ExtraTime.IntegrationTests.Fixtures;

public sealed class DatabaseFixture
{
    private PostgreSqlContainer? _container;
    private Respawner _respawner = null!;
    private NpgsqlConnection _connection = null!;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public bool UseInMemory { get; private set; }
    public string ConnectionString => UseInMemory || _container == null ? string.Empty : _container.GetConnectionString();

    public async Task EnsureInitializedAsync()
    {
        if (_initialized)
            return;

        await _initLock.WaitAsync();
        try
        {
            if (_initialized)
                return;

            // Check for environment variable configuration
            var dbType = Environment.GetEnvironmentVariable("TEST_DATABASE_TYPE");
            if (string.Equals(dbType, "InMemory", StringComparison.OrdinalIgnoreCase))
            {
                UseInMemory = true;
                _initialized = true;
                return;
            }

            try
            {
                // Use constructor with image parameter as per warning
                _container = new PostgreSqlBuilder("postgres:17-alpine")
                    .WithDatabase("extratime_test")
                    .WithUsername("postgres")
                    .WithPassword("postgres")
                    .Build();

                await _container.StartAsync();

                var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseNpgsql(ConnectionString)
                    .Options;


                var mockCurrentUserService = Substitute.For<ICurrentUserService>();
                var mockMediator = Substitute.For<IMediator>();
                await using var context = new ApplicationDbContext(options, mockCurrentUserService, mockMediator);
                await context.Database.MigrateAsync();

                _connection = new NpgsqlConnection(ConnectionString);
                await _connection.OpenAsync();

                _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
                {
                    DbAdapter = DbAdapter.Postgres,
                    SchemasToInclude = ["public"]
                });
            }
            catch (Exception ex)
            {
                // Docker not available, fallback to InMemory
                await Console.Error.WriteLineAsync($"[DatabaseFixture] Failed to initialize PostgreSqlContainer: {ex}");
                UseInMemory = true;
            }

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task ResetDatabaseAsync()
    {
        if (UseInMemory) return;
        
        if (_respawner != null && _connection != null)
        {
            await _respawner.ResetAsync(_connection);
        }
    }

    public async Task DisposeAsync()
    {
        if (UseInMemory) return;
        
        if (_connection != null)
            await _connection.DisposeAsync();
        
        if (_container != null)
            await _container.DisposeAsync();
    }
}
