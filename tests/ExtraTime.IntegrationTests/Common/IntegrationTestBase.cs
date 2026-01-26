using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Infrastructure.Data;
using ExtraTime.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.IntegrationTests.Common;

public abstract class IntegrationTestBase
{
    private static readonly DatabaseFixture Fixture = new();
    protected ApplicationDbContext Context { get; private set; } = null!;
    protected ICurrentUserService CurrentUserService { get; private set; } = null!;

    [Before(Test)]
    public async Task InitializeAsync()
    {
        await Fixture.EnsureInitializedAsync();
        await Fixture.ResetDatabaseAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(Fixture.ConnectionString)
            .Options;

        CurrentUserService = Substitute.For<ICurrentUserService>();
        Context = new ApplicationDbContext(options, CurrentUserService);
    }

    [After(Test)]
    public async Task DisposeAsync()
    {
        await Context.DisposeAsync();
    }

    protected void SetCurrentUser(Guid userId)
    {
        CurrentUserService.UserId.Returns(userId);
        CurrentUserService.IsAuthenticated.Returns(true);
    }
}
