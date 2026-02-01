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
/// Manages a single test database for integration tests.
/// Uses Testcontainers for SQL Server with Respawn for fast resets.
/// Tests run sequentially (maxParallelTests=1), so no locking needed.
/// </summary>
public sealed class DatabaseFixture : IAsyncDisposable
{
    private static readonly TimeSpan ResetTimeout = TimeSpan.FromSeconds(30);

    // Unique session ID to avoid database conflicts with reused containers
    private static readonly string SessionId = Guid.NewGuid().ToString("N")[..8];
    private static readonly string DatabaseName = $"ExtraTime_{SessionId}";

    private MsSqlContainer? _container;
    private string _connectionString = string.Empty;
    private Respawner? _respawner;

    private bool _isInitialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly SemaphoreSlim _leaseLock = new(1, 1);
    private bool _disposed;

    public bool UseInMemory { get; private set; }

    /// <summary>
    /// Initializes the fixture. Thread-safe and idempotent.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (_isInitialized) return;

            var dbType = Environment.GetEnvironmentVariable("TEST_DATABASE_TYPE");
            if (string.Equals(dbType, "InMemory", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("[DatabaseFixture] Using InMemory mode (via environment variable)");
                UseInMemory = true;
                _isInitialized = true;
                return;
            }

            try
            {
                await InitializeSqlServerAsync();
                _isInitialized = true;
                Console.WriteLine($"[DatabaseFixture] SQL Server ready. Database: {DatabaseName}");
            }
            catch (Exception ex)
            {
                var errorLog = Path.Combine(Path.GetTempPath(), "extratime_test_error.log");
                var errorMessage = $"""
                    [DatabaseFixture] SQL Server init failed at {DateTime.Now}
                    Error type: {ex.GetType().Name}
                    Error message: {ex.Message}
                    Inner exception: {ex.InnerException?.Message ?? "None"}
                    Stack trace: {ex.StackTrace}
                    """;
                File.WriteAllText(errorLog, errorMessage);
                Console.Error.WriteLine($"[DatabaseFixture] SQL Server init failed. See {errorLog}");
                throw;
            }
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task InitializeSqlServerAsync()
    {
        Console.WriteLine("[DatabaseFixture] Starting SQL Server container...");

        _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithReuse(true)
            .Build();

        await _container.StartAsync();

        var baseConnectionString = _container.GetConnectionString();
        Console.WriteLine("[DatabaseFixture] Container started. Creating test database...");

        // Create the database
        await using (var masterConn = new SqlConnection(baseConnectionString))
        {
            await masterConn.OpenAsync();
            await using var cmd = masterConn.CreateCommand();
            cmd.CommandText = $"CREATE DATABASE [{DatabaseName}]";
            await cmd.ExecuteNonQueryAsync();
        }

        // Build connection string
        var builder = new SqlConnectionStringBuilder(baseConnectionString)
        {
            InitialCatalog = DatabaseName,
            Pooling = true,
            MinPoolSize = 0,
            MaxPoolSize = 100, // Increased for stability
            ConnectTimeout = 60 // Increased for stability
        };
        _connectionString = builder.ConnectionString;

        // Run migrations
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        await using var context = new ApplicationDbContext(
            options,
            Substitute.For<ICurrentUserService>(),
            Substitute.For<IMediator>());

        await context.Database.MigrateAsync();

        // Create Respawner for fast resets
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            WithReseed = true
        });

        Console.WriteLine($"[DatabaseFixture] Database {DatabaseName} initialized");
    }

    /// <summary>
    /// Gets a database lease for a test.
    /// Uses a semaphore to ensure only one test uses the database at a time.
    /// </summary>
    public async Task<DatabaseLease> AcquireDatabaseAsync(CancellationToken cancellationToken = default)
    {
        // Use a generous timeout for the lease (tests shouldn't take this long)
        var timeout = TimeSpan.FromMinutes(10);
        var acquired = await _leaseLock.WaitAsync(timeout, cancellationToken);
        
        if (!acquired)
        {
            throw new TimeoutException("[DatabaseFixture] Timed out waiting for database lease lock");
        }

        try
        {
            if (!UseInMemory)
            {
                // Reset the database before returning
                await ResetDatabaseAsync(cancellationToken);
            }
            
            return new DatabaseLease(_leaseLock, UseInMemory, UseInMemory ? null : _connectionString);
        }
        catch
        {
            _leaseLock.Release();
            throw;
        }
    }

    private async Task ResetDatabaseAsync(CancellationToken cancellationToken)
    {
        if (_respawner == null) return;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(ResetTimeout);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cts.Token);
        await _respawner.ResetAsync(connection);
    }

    public string ConnectionString => _connectionString;

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _initLock.Dispose();

        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }
}

/// <summary>
/// Represents access to the database for a single test.
/// Releases the lease lock when disposed.
/// </summary>
public sealed class DatabaseLease : IDisposable
{
    private readonly SemaphoreSlim _leaseLock;
    public bool IsInMemory { get; }
    public string? ConnectionString { get; }

    public DatabaseLease(SemaphoreSlim leaseLock, bool isInMemory, string? connectionString)
    {
        _leaseLock = leaseLock;
        IsInMemory = isInMemory;
        ConnectionString = connectionString;
    }

    public void Dispose()
    {
        _leaseLock.Release();
    }
}
