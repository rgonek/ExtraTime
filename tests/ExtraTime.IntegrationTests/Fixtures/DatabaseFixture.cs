using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Infrastructure.Data;
using Mediator;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Respawn;
using Testcontainers.MsSql;

namespace ExtraTime.IntegrationTests.Fixtures;

/// <summary>
/// Manages a pool of test databases for parallel test execution.
/// Uses Testcontainers for SQL Server and Respawn for fast database resets.
/// </summary>
public sealed class DatabaseFixture : IAsyncDisposable
{
    private static readonly int PoolSize = Environment.ProcessorCount > 4
        ? 4
        : Math.Max(2, Environment.ProcessorCount);

    private MsSqlContainer? _container;
    private readonly Respawner?[] _respawners;
    private readonly SqlConnection?[] _connections;
    private readonly SemaphoreSlim[] _poolLocks;
    private int _currentIndex = -1;

    private bool _isInitialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public bool UseInMemory { get; private set; }
    public string BaseConnectionString { get; private set; } = string.Empty;

    public DatabaseFixture()
    {
        _respawners = new Respawner?[PoolSize];
        _connections = new SqlConnection?[PoolSize];
        _poolLocks = Enumerable.Range(0, PoolSize)
            .Select(_ => new SemaphoreSlim(1, 1))
            .ToArray();
    }

    /// <summary>
    /// Initializes the fixture. Should be called once before any tests run.
    /// Thread-safe and idempotent.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        await _initLock.WaitAsync();
        try
        {
            if (_isInitialized)
                return;

            var dbType = Environment.GetEnvironmentVariable("TEST_DATABASE_TYPE");
            if (string.Equals(dbType, "InMemory", StringComparison.OrdinalIgnoreCase))
            {
                UseInMemory = true;
                _isInitialized = true;
                return;
            }

            try
            {
                await InitializeSqlServerAsync();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync(
                    $"[DatabaseFixture] Failed to initialize SQL Server container, falling back to InMemory: {ex.Message}");
                UseInMemory = true;
                _isInitialized = true;
            }
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task InitializeSqlServerAsync()
    {
        _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithReuse(true)
            .Build();

        await _container.StartAsync();
        BaseConnectionString = _container.GetConnectionString();

        // Initialize all databases in the pool in parallel
        var initTasks = Enumerable.Range(0, PoolSize)
            .Select(InitializeDatabaseAsync)
            .ToArray();

        await Task.WhenAll(initTasks);
    }

    private async Task InitializeDatabaseAsync(int index)
    {
        var dbName = $"ExtraTime_Test_{index}";

        // Create database
        await using (var masterConnection = new SqlConnection(BaseConnectionString))
        {
            await masterConnection.OpenAsync();
            await using var cmd = new SqlCommand(
                $"IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = '{dbName}') CREATE DATABASE [{dbName}]",
                masterConnection);
            await cmd.ExecuteNonQueryAsync();
        }

        // Connect to specific database and run migrations
        var dbConnectionString = $"{BaseConnectionString};Database={dbName}";
        _connections[index] = new SqlConnection(dbConnectionString);
        await _connections[index]!.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(dbConnectionString)
            .Options;

        var mockCurrentUserService = Substitute.For<ICurrentUserService>();
        var mockMediator = Substitute.For<IMediator>();
        await using var context = new ApplicationDbContext(options, mockCurrentUserService, mockMediator);
        await context.Database.MigrateAsync();

        _respawners[index] = await Respawner.CreateAsync(_connections[index]!, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer
        });
    }

    /// <summary>
    /// Acquires a database from the pool. The returned item must be disposed after use.
    /// </summary>
    public async Task<DatabasePoolItem> AcquireDatabaseAsync()
    {
        if (UseInMemory)
        {
            return new DatabasePoolItem(-1, string.Empty, null, null, null);
        }

        // Round-robin selection with atomic increment
        var index = (uint)Interlocked.Increment(ref _currentIndex) % PoolSize;

        // Wait for this database slot to be available
        await _poolLocks[index].WaitAsync();

        var dbName = $"ExtraTime_Test_{index}";
        var connectionString = $"{BaseConnectionString};Database={dbName}";

        // Reset database state before returning
        if (_respawners[index] != null && _connections[index] != null)
        {
            await _respawners[index]!.ResetAsync(_connections[index]!);
        }

        return new DatabasePoolItem(
            (int)index,
            connectionString,
            _respawners[index],
            _connections[index],
            _poolLocks[index]);
    }

    public async ValueTask DisposeAsync()
    {
        if (UseInMemory)
            return;

        foreach (var connection in _connections)
        {
            if (connection != null)
                await connection.DisposeAsync();
        }

        foreach (var lockItem in _poolLocks)
        {
            lockItem.Dispose();
        }

        _initLock.Dispose();

        if (_container != null)
            await _container.DisposeAsync();
    }
}

/// <summary>
/// Represents a database acquired from the pool. Must be disposed after use.
/// </summary>
public sealed class DatabasePoolItem : IAsyncDisposable
{
    public int PoolIndex { get; }
    public string ConnectionString { get; }
    public Respawner? Respawner { get; }
    public SqlConnection? Connection { get; }
    private readonly SemaphoreSlim? _lock;
    private bool _released;

    public DatabasePoolItem(
        int poolIndex,
        string connectionString,
        Respawner? respawner,
        SqlConnection? connection,
        SemaphoreSlim? lockItem)
    {
        PoolIndex = poolIndex;
        ConnectionString = connectionString;
        Respawner = respawner;
        Connection = connection;
        _lock = lockItem;
    }

    public void Release()
    {
        if (_released)
            return;

        _released = true;
        _lock?.Release();
    }

    public ValueTask DisposeAsync()
    {
        Release();
        return ValueTask.CompletedTask;
    }
}
