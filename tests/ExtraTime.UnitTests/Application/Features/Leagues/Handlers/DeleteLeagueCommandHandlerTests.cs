using ExtraTime.Application.Features.Leagues.Commands.DeleteLeague;
using ExtraTime.Domain.Common;
using ExtraTime.UnitTests.Common;
using ExtraTime.UnitTests.Helpers;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using TUnit.Core;

namespace ExtraTime.UnitTests.Application.Features.Leagues.Handlers;

[NotInParallel]
public sealed class DeleteLeagueCommandHandlerTests : HandlerTestBase
{
    private readonly DeleteLeagueCommandHandler _handler;
    private readonly DateTime _now = new(2026, 1, 26, 12, 0, 0, DateTimeKind.Utc);

    public DeleteLeagueCommandHandlerTests()
    {
        _handler = new DeleteLeagueCommandHandler(Context, CurrentUserService);
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
    public async Task Handle_OwnerDeletingLeague_ReturnsSuccess()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var leagueId = Guid.NewGuid();
        var league = new LeagueBuilder()
            .WithId(leagueId)
            .WithOwnerId(ownerId)
            .Build();

        SetCurrentUser(ownerId);

        var leagues = new List<Domain.Entities.League> { league }.AsQueryable();
        var mockLeagues = CreateMockDbSet(leagues);
        Context.Leagues.Returns(mockLeagues);

        var command = new DeleteLeagueCommand(leagueId);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        Context.Leagues.Received(1).Remove(Arg.Any<Domain.Entities.League>());
        await Context.Received(1).SaveChangesAsync(CancellationToken);
    }

    [Test]
    public async Task Handle_LeagueNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetCurrentUser(userId);

        var mockLeagues = CreateMockDbSet(new List<Domain.Entities.League>().AsQueryable());
        Context.Leagues.Returns(mockLeagues);

        var command = new DeleteLeagueCommand(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task Handle_NotTheOwner_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var leagueId = Guid.NewGuid();
        var league = new LeagueBuilder()
            .WithId(leagueId)
            .WithOwnerId(ownerId)
            .Build();

        SetCurrentUser(otherUserId);

        var leagues = new List<Domain.Entities.League> { league }.AsQueryable();
        var mockLeagues = CreateMockDbSet(leagues);
        Context.Leagues.Returns(mockLeagues);

        var command = new DeleteLeagueCommand(leagueId);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }
}
