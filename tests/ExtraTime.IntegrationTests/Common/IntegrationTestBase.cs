using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Infrastructure.Data;
using ExtraTime.IntegrationTests.Fixtures;
using Mediator;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.IntegrationTests.Common;

/// <summary>
/// Base class for integration tests. Provides access to database context and common test utilities.
/// The database fixture is initialized once per test assembly via GlobalHooks.
/// </summary>
public abstract class IntegrationTestBase : IAsyncDisposable
{
    private DatabasePoolItem? _poolItem;

    protected ApplicationDbContext Context { get; private set; } = null!;
    protected ICurrentUserService CurrentUserService { get; private set; } = null!;
    protected IMediator Mediator { get; private set; } = null!;

    [Before(Test)]
    public async Task SetupAsync()
    {
        try
        {
            // Acquire a database from the pool (fixture already initialized by GlobalHooks)
            _poolItem = await GlobalHooks.Fixture.AcquireDatabaseAsync();

            DbContextOptions<ApplicationDbContext> options;

            if (GlobalHooks.Fixture.UseInMemory)
            {
                // Each test gets a unique in-memory database
                options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options;
            }
            else
            {
                options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseSqlServer(_poolItem.ConnectionString)
                    .Options;
            }

            CurrentUserService = Substitute.For<ICurrentUserService>();
            Mediator = Substitute.For<IMediator>();
            Context = new ApplicationDbContext(options, CurrentUserService, Mediator);

            if (GlobalHooks.Fixture.UseInMemory)
            {
                await Context.Database.EnsureCreatedAsync();
            }
        }
        catch
        {
            // Release the pool item if setup fails
            _poolItem?.Release();
            _poolItem = null;
            throw;
        }
    }

    [After(Test)]
    public async ValueTask TeardownAsync()
    {
        if (Context != null)
        {
            await Context.DisposeAsync();
            Context = null!;
        }

        if (_poolItem != null)
        {
            _poolItem.Release();
            _poolItem = null;
        }
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await TeardownAsync();
    }

    /// <summary>
    /// Sets the current user for the test. Use this to simulate authenticated requests.
    /// </summary>
    protected void SetCurrentUser(Guid userId)
    {
        CurrentUserService.UserId.Returns(userId);
        CurrentUserService.IsAuthenticated.Returns(true);
    }
}
