using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Infrastructure.Data;
using ExtraTime.IntegrationTests.Fixtures;
using Mediator;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.IntegrationTests.Common;

public abstract class IntegrationTestBase
{
    private static readonly DatabaseFixture Fixture = new();
    private static readonly SemaphoreSlim DatabaseLock = new(1, 1);
    protected ApplicationDbContext Context { get; private set; } = null!;
    protected ICurrentUserService CurrentUserService { get; private set; } = null!;
    protected IMediator Mediator { get; private set; } = null!;

    [Before(Test)]
    public async Task InitializeAsync()
    {
        await DatabaseLock.WaitAsync();
        await Fixture.EnsureInitializedAsync();
        await Fixture.ResetDatabaseAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(Fixture.ConnectionString)
            .Options;

        CurrentUserService = Substitute.For<ICurrentUserService>();
        Mediator = Substitute.For<IMediator>();
        Context = new ApplicationDbContext(options, CurrentUserService, Mediator);
    }

    [After(Test)]
    public async Task DisposeAsync()
    {
        await Context.DisposeAsync();
        DatabaseLock.Release();
    }

    protected void SetCurrentUser(Guid userId)
    {
        CurrentUserService.UserId.Returns(userId);
        CurrentUserService.IsAuthenticated.Returns(true);
    }
}
