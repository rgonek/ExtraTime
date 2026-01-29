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
    private static readonly SemaphoreSlim DatabaseLock = new(1, 1);
    
    private static DatabaseFixture Fixture
    {
        get
        {
            if (_fixture == null)
                _fixture = new DatabaseFixture();
            return _fixture;
        }
    }

    private bool _lockTaken;

    protected ApplicationDbContext Context { get; private set; } = null!;
    protected ICurrentUserService CurrentUserService { get; private set; } = null!;
    protected IMediator Mediator { get; private set; } = null!;

    [Before(Test)]
    public async Task InitializeAsync()
    {
        await DatabaseLock.WaitAsync();
        _lockTaken = true;
        
        try 
        {
            await Fixture.EnsureInitializedAsync();

            DbContextOptions<ApplicationDbContext> options;

            if (Fixture.UseInMemory)
            {
                options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options;
            }
            else
            {
                await Fixture.ResetDatabaseAsync();
                options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseSqlServer(Fixture.ConnectionString)
                    .Options;
            }

            CurrentUserService = Substitute.For<ICurrentUserService>();
            Mediator = Substitute.For<IMediator>();
            Context = new ApplicationDbContext(options, CurrentUserService, Mediator);
            
            if (Fixture.UseInMemory)
            {
                await Context.Database.EnsureCreatedAsync();
            }
        }
        catch (Exception)
        {
            if (_lockTaken)
            {
                DatabaseLock.Release();
                _lockTaken = false;
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
        
        if (_lockTaken)
        {
            DatabaseLock.Release();
            _lockTaken = false;
        }
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
