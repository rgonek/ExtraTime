using ExtraTime.IntegrationTests.Fixtures;

namespace ExtraTime.IntegrationTests.Common;

/// <summary>
/// Global hooks for test assembly lifecycle. Initializes shared resources once before any tests run.
/// </summary>
public static class GlobalHooks
{
    private static readonly DatabaseFixture _fixture = new();
    private static bool _initialized;
    private static readonly SemaphoreSlim _initLock = new(1, 1);

    // Timeout for fixture initialization (container startup can be slow)
    private static readonly TimeSpan InitTimeout = TimeSpan.FromMinutes(5);

    public static DatabaseFixture Fixture => _fixture;

    /// <summary>
    /// Initializes the database fixture before any tests run.
    /// Called automatically by TUnit's assembly hook.
    /// </summary>
    [Before(Assembly)]
    public static async Task InitializeAsync()
    {
        if (_initialized) return;

        var acquired = await _initLock.WaitAsync(InitTimeout);
        if (!acquired)
        {
            throw new TimeoutException(
                $"[GlobalHooks] Timed out waiting for initialization lock after {InitTimeout.TotalMinutes} minutes");
        }

        try
        {
            if (_initialized) return;

            Console.WriteLine("[GlobalHooks] Initializing test fixture...");
            await _fixture.InitializeAsync();
            _initialized = true;
            Console.WriteLine("[GlobalHooks] Test fixture initialized successfully");
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <summary>
    /// Disposes the database fixture after all tests have completed.
    /// Called automatically by TUnit's assembly hook.
    /// </summary>
    [After(Assembly)]
    public static async Task CleanupAsync()
    {
        Console.WriteLine("[GlobalHooks] Cleaning up test fixture...");
        await _fixture.DisposeAsync();
        _initLock.Dispose();
        Console.WriteLine("[GlobalHooks] Cleanup complete");
    }
}
