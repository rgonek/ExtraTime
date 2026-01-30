using ExtraTime.Application.Features.Leagues.Commands.LeaveLeague;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Enums;
using ExtraTime.UnitTests.Common;
using ExtraTime.UnitTests.Helpers;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.UnitTests.Application.Features.Leagues.Handlers;

public sealed class LeaveLeagueCommandHandlerTests : HandlerTestBase
{
    private readonly LeaveLeagueCommandHandler _handler;
    private readonly DateTime _now = new(2026, 1, 26, 12, 0, 0, DateTimeKind.Utc);

    public LeaveLeagueCommandHandlerTests()
    {
        _handler = new LeaveLeagueCommandHandler(Context, CurrentUserService);
    }

    [Before(Test)]
    public void Setup()
    {
        Clock.Current = new FakeClock(_now);
    }

    [After(Test)]
    public void Cleanup()
    {
        Clock.Current = null!;
    }

    [Test]
    public async Task Handle_LeagueNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetCurrentUser(userId);

        var mockLeagues = CreateMockDbSet(new List<Domain.Entities.League>().AsQueryable());
        Context.Leagues.Returns(mockLeagues);

        var command = new LeaveLeagueCommand(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }
}
