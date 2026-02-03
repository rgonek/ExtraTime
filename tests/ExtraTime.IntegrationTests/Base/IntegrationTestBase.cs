using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Infrastructure.Data;
using ExtraTime.IntegrationTests.Factories;
using Mediator;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.IntegrationTests.Base;

public abstract class IntegrationTestBase : IAsyncDisposable
{
    private ITestDatabase? _db;

    // Primary Context for the test (Arrange/Act)
    protected ApplicationDbContext Context { get; private set; } = null!;
    protected ICurrentUserService CurrentUserService { get; private set; } = null!;
    protected IMediator Mediator { get; private set; } = null!;

    [Before(Test)]
    public async Task SetupAsync()
    {
        // 1. Get a database (InMemory or SQL Server based on Config)
        _db = TestConfig.CreateDatabase();

        // 2. Initialize (Create DB/Schema)
        await _db.InitializeAsync();

        // 3. Setup Mocks
        CurrentUserService = Substitute.For<ICurrentUserService>();
        Mediator = Substitute.For<IMediator>();

        // 4. Create Context
        Context = CreateDbContext();
    }

    [After(Test)]
    public async Task TeardownAsync()
    {
        if (Context != null)
        {
            await Context.DisposeAsync();
        }

        if (_db != null)
        {
            await _db.DisposeAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await TeardownAsync();
    }

    protected ApplicationDbContext CreateDbContext()
    {
        if (_db == null) throw new InvalidOperationException("Database not initialized");

        // We recreate the context but pass our specific Mocks
        // The factory CreateContext uses generic mocks, so we might want to manually create it here
        // using the connection string from _db?
        // Actually, _db.CreateContext() creates one. We might need to replace the services if we want to mock them.

        // Let's modify ITestDatabase to allow creating context with specific mocks or just handle it here.
        // Easier: The Factory returns a context. But we want to inject OUR Mediator/User.

        // If we look at SqlServerTestDatabase.CreateContext(), it creates new options.
        // We can just reuse those options if we expose them?
        // Or just ask the factory for the connection string/options and build it here?

        // Refactoring ITestDatabase to genericize context creation might be better.
        // But for now, let's just assume we can't easily inject into the factory's internal creation 
        // without changing the interface.

        // Actually, ITestDatabase.CreateContext() is used in InitializeAsync() to run migrations.
        // For the TEST, we want to control the mocks.
        // So let's add a helper here using the connection string.

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        if (TestConfig.UseSqlServer)
        {
            optionsBuilder.UseSqlServer(_db.ConnectionString);
        }
        else
        {
            optionsBuilder.UseInMemoryDatabase(_db.ConnectionString) // For InMemory, conn string is the DB Name
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
        }

        return new ApplicationDbContext(optionsBuilder.Options, CurrentUserService, Mediator);
    }

    protected void SetCurrentUser(Guid userId)
    {
        CurrentUserService.UserId.Returns(userId);
        CurrentUserService.IsAuthenticated.Returns(true);
    }
}
