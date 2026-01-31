using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Infrastructure.Data;
using ExtraTime.IntegrationTests.Fixtures;
using Mediator;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.IntegrationTests.Common;

public abstract class IntegrationTestBase : IAsyncDisposable
{
    private static DatabaseFixture? _fixture;
    private DatabasePoolItem? _poolItem;
    private bool _initialized;
    
    private static DatabaseFixture Fixture
    {
        get
        {
            if (_fixture == null)
                _fixture = new DatabaseFixture();
            return _fixture;
        }
    }

    protected ApplicationDbContext Context { get; private set; } = null!;
    protected ICurrentUserService CurrentUserService { get; private set; } = null!;
    protected IMediator Mediator { get; private set; } = null!;

    [Before(Test)]
    public async Task InitializeAsync()
    {
        try 
        {
            // Acquire a database from the pool
            _poolItem = await Fixture.AcquireDatabaseAsync();

            DbContextOptions<ApplicationDbContext> options;

            if (Fixture.UseInMemory)
            {
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
            
            if (Fixture.UseInMemory)
            {
                await Context.Database.EnsureCreatedAsync();
            }
            
            _initialized = true;
        }
        catch (Exception)
        {
            if (_poolItem != null)
            {
                _poolItem.Release();
                _poolItem = null;
            }
            throw;
        }
    }

    [After(Test)]
    public async ValueTask DisposeAsync()
    {
        if (Context != null)
        {
            await Context.DisposeAsync();
        }
        
        if (_poolItem != null)
        {
            _poolItem.Release();
            _poolItem = null;
        }
        
        _initialized = false;
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await DisposeAsync();
    }

    protected void SetCurrentUser(Guid userId)
    {
        CurrentUserService.UserId.Returns(userId);
        CurrentUserService.IsAuthenticated.Returns(true);
    }
}
