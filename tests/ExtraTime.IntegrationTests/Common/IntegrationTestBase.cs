using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Infrastructure.Data;
using ExtraTime.IntegrationTests.Attributes;
using ExtraTime.IntegrationTests.Fixtures;
using Mediator;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using TUnit.Core;

namespace ExtraTime.IntegrationTests.Common;

/// <summary>
/// Base class for integration tests. Provides access to database context and common test utilities.
/// The database fixture is initialized once per test assembly via GlobalHooks.
/// </summary>
public abstract class IntegrationTestBase : IAsyncDisposable
{
    private DatabaseLease? _lease;
    private bool _leaseAcquired;
    private DbContextOptions<ApplicationDbContext> _options = null!;

    protected ApplicationDbContext Context { get; private set; } = null!;
    protected ICurrentUserService CurrentUserService { get; private set; } = null!;
    protected IMediator Mediator { get; private set; } = null!;

    /// <summary>
    /// Creates a fresh DbContext instance using the same configuration as the current test.
    /// </summary>
    protected ApplicationDbContext CreateDbContext()
    {
        return new ApplicationDbContext(_options, CurrentUserService, Mediator);
    }

    /// <summary>
    /// Override this to true in test classes that require a physical SQL Server database.
    /// Defaults to false (In-Memory).
    /// </summary>
    protected virtual bool UseSqlDatabase => false;

    [Before(Test)]
    public async Task SetupAsync()
    {
        var needsSql = UseSqlDatabase || RequiresDatabaseCheck();

        // Skip tests that require a real database when running in InMemory mode
        if (GlobalHooks.Fixture.UseInMemory && needsSql)
        {
            Skip.Test("Test requires a real database and is running in InMemory mode");
        }

        // Acquire a database lease (with timeout protection and synchronization)
        _lease = await GlobalHooks.Fixture.AcquireDatabaseAsync(needsSql);
        _leaseAcquired = true;

        try
        {
            CurrentUserService = Substitute.For<ICurrentUserService>();
            Mediator = Substitute.For<IMediator>();
            
            if (_lease.IsInMemory)
            {
                // Each test gets a unique in-memory database
                _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase($"Test_{Guid.NewGuid()}")
                    .ConfigureWarnings(w => w.Ignore(
                        Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                    .Options;

                Context = CreateDbContext();
                await Context.Database.EnsureCreatedAsync();
            }
            else
            {
                // SQL Server mode
                _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseSqlServer(_lease.ConnectionString!, sqlOptions =>
                    {
                        sqlOptions.CommandTimeout(30);
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
                    })
                    .Options;

                Context = CreateDbContext();
            }
        }
        catch
        {
            // Ensure lease is released on any setup failure
            ReleaseLease();
            throw;
        }
    }


    [After(Test)]
    public async ValueTask TeardownAsync()
    {
        // Dispose DbContext first
        if (Context != null!)
        {
            try
            {
                await Context.DisposeAsync();
            }
            catch
            {
                // Swallow disposal errors - we don't want teardown to fail
            }
            Context = null!;
        }

        // Always release the lease
        ReleaseLease();
    }

    private void ReleaseLease()
    {
        if (_leaseAcquired && _lease != null)
        {
            try
            {
                _lease.Dispose();
            }
            catch
            {
                // Swallow - we must not fail during cleanup
            }
            _lease = null;
            _leaseAcquired = false;
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

    /// <summary>
    /// Checks if the current test requires a real database by examining
    /// custom properties set via TestCategoryAttribute.
    /// </summary>
    private static bool RequiresDatabaseCheck()
    {
        var customProperties = TestContext.Current?.Metadata?.TestDetails.CustomProperties;
        if (customProperties == null)
            return false;

        if (customProperties.TryGetValue("Category", out var categories))
        {
            return categories.Contains(TestCategories.RequiresDatabase);
        }

        return false;
    }
}
