using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Infrastructure.Data;
using Mediator;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Respawn;
using Testcontainers.MsSql;

namespace ExtraTime.IntegrationTests.Fixtures;

public sealed class DatabaseFixture : IAsyncDisposable
{
    private static readonly int PoolSize = 4; // Number of parallel databases
    private MsSqlContainer? _container;
    private Respawner[] _respawners = new Respawner[PoolSize];
    private SqlConnection[] _connections = new SqlConnection[PoolSize];
    private bool[] _initialized = new bool[PoolSize];
    private int _currentIndex = -1;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly SemaphoreSlim[] _poolLocks = Enumerable.Range(0, PoolSize).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

    public bool UseInMemory { get; private set; }
    public string ConnectionString => UseInMemory || _container == null ? string.Empty : _container.GetConnectionString();

    /// <summary>
    /// Gets a database from the pool. Must be released after use.
    /// </summary>
    public async Task<DatabasePoolItem> AcquireDatabaseAsync()
    {
        if (UseInMemory)
        {
            return new DatabasePoolItem(-1, string.Empty, null, null, null);
        }

        await _initLock.WaitAsync();
        try
        {
            await EnsureInitializedAsync();
        }
        finally
        {
            _initLock.Release();
        }

        // Round-robin selection
        var index = Interlocked.Increment(ref _currentIndex) % PoolSize;
        
        await _poolLocks[index].WaitAsync();
        
        var dbName = $"ExtraTime_Test_{index}";
        var connectionString = $"{ConnectionString};Database={dbName}";
        
        // Ensure database exists
        using (var masterConnection = new SqlConnection(ConnectionString))
        {
            await masterConnection.OpenAsync();
            using var cmd = new SqlCommand(
                $"IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = '{dbName}') CREATE DATABASE [{dbName}]", 
                masterConnection);
            await cmd.ExecuteNonQueryAsync();
        }

        // Reset database before use
        if (_respawners[index] != null && _connections[index] != null)
        {
            await _respawners[index].ResetAsync(_connections[index]);
        }

        return new DatabasePoolItem(index, connectionString, _respawners[index], _connections[index], _poolLocks[index]);
    }

    public async Task EnsureInitializedAsync()
    {
        if (_initialized[0])
            return;

        await _initLock.WaitAsync();
        try
        {
            if (_initialized[0])
                return;

            // Check for environment variable configuration
            var dbType = Environment.GetEnvironmentVariable("TEST_DATABASE_TYPE");
            if (string.Equals(dbType, "InMemory", StringComparison.OrdinalIgnoreCase))
            {
                UseInMemory = true;
                for (int i = 0; i < PoolSize; i++)
                    _initialized[i] = true;
                return;
            }

            try
            {
                _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
                    .WithReuse(true) // Reuse container across test runs
                    .Build();

                await _container.StartAsync();

                // Initialize all databases in the pool
                for (int i = 0; i < PoolSize; i++)
                {
                    var dbName = $"ExtraTime_Test_{i}";
                    
                    // Create database
                    using (var masterConnection = new SqlConnection(ConnectionString))
                    {
                        await masterConnection.OpenAsync();
                        using var cmd = new SqlCommand(
                            $"IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = '{dbName}') CREATE DATABASE [{dbName}]", 
                            masterConnection);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // Connect to specific database and run migrations
                    var dbConnectionString = $"{ConnectionString};Database={dbName}";
                    _connections[i] = new SqlConnection(dbConnectionString);
                    await _connections[i].OpenAsync();

                    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                        .UseSqlServer(dbConnectionString)
                        .Options;

                    var mockCurrentUserService = Substitute.For<ICurrentUserService>();
                    var mockMediator = Substitute.For<IMediator>();
                    await using var context = new ApplicationDbContext(options, mockCurrentUserService, mockMediator);
                    await context.Database.MigrateAsync();

                    _respawners[i] = await Respawner.CreateAsync(_connections[i], new RespawnerOptions
                    {
                        DbAdapter = DbAdapter.SqlServer
                    });

                    _initialized[i] = true;
                }
            }
            catch (Exception ex)
            {
                // Docker not available, fallback to InMemory
                await Console.Error.WriteLineAsync($"[DatabaseFixture] Failed to initialize MsSqlContainer: {ex}");
                UseInMemory = true;
                for (int i = 0; i < PoolSize; i++)
                    _initialized[i] = true;
            }
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task ResetDatabaseAsync(int poolIndex)
    {
        if (UseInMemory || poolIndex < 0 || poolIndex >= PoolSize) return;

        if (_respawners[poolIndex] != null && _connections[poolIndex] != null)
        {
            await _respawners[poolIndex].ResetAsync(_connections[poolIndex]);
        }
    }

    public void ReleaseDatabase(int poolIndex)
    {
        if (poolIndex >= 0 && poolIndex < PoolSize)
        {
            _poolLocks[poolIndex].Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (UseInMemory) return;

        for (int i = 0; i < PoolSize; i++)
        {
            if (_connections[i] != null)
                await _connections[i].DisposeAsync();
        }

        if (_container != null)
            await _container.DisposeAsync();
    }
}

public sealed class DatabasePoolItem : IAsyncDisposable
{
    public int PoolIndex { get; }
    public string ConnectionString { get; }
    public Respawner? Respawner { get; }
    public SqlConnection? Connection { get; }
    private readonly SemaphoreSlim? _lock;

    public DatabasePoolItem(int poolIndex, string connectionString, Respawner? respawner, SqlConnection? connection, SemaphoreSlim? lockItem)
    {
        PoolIndex = poolIndex;
        ConnectionString = connectionString;
        Respawner = respawner;
        Connection = connection;
        _lock = lockItem;
    }

    public void Release()
    {
        _lock?.Release();
    }

    public ValueTask DisposeAsync()
    {
        Release();
        return ValueTask.CompletedTask;
    }
}
