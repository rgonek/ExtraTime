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

    public static DatabaseFixture Fixture => _fixture;

    /// <summary>
    /// Initializes the database fixture before any tests run.
    /// Called automatically by TUnit's assembly hook.
    /// </summary>
    [Before(Assembly)]
    public static async Task InitializeAsync()
    {
        if (_initialized)
            return;

        await _initLock.WaitAsync();
        try
        {
            if (_initialized)
                return;

            await _fixture.InitializeAsync();
            _initialized = true;
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
        await _fixture.DisposeAsync();
    }
}
