using ExtraTime.Application.Features.Leagues.Commands.UpdateLeague;
using ExtraTime.Domain.Common;
using ExtraTime.UnitTests.Common;
using ExtraTime.UnitTests.Helpers;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using TUnit.Core;

namespace ExtraTime.UnitTests.Application.Features.Leagues.Handlers;

[NotInParallel]
public sealed class UpdateLeagueCommandHandlerTests : HandlerTestBase
{
    private readonly UpdateLeagueCommandHandler _handler;
    private readonly DateTime _now = new(2026, 1, 26, 12, 0, 0, DateTimeKind.Utc);

    public UpdateLeagueCommandHandlerTests()
    {
        _handler = new UpdateLeagueCommandHandler(Context, CurrentUserService);
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

        var command = new UpdateLeagueCommand(
            LeagueId: Guid.NewGuid(),
            Name: "New Name",
            Description: null,
            IsPublic: false,
            MaxMembers: 20,
            ScoreExactMatch: 3,
            ScoreCorrectResult: 1,
            BettingDeadlineMinutes: 5,
            AllowedCompetitionIds: null);

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

        var command = new UpdateLeagueCommand(
            LeagueId: leagueId,
            Name: "New Name",
            Description: null,
            IsPublic: false,
            MaxMembers: 20,
            ScoreExactMatch: 3,
            ScoreCorrectResult: 1,
            BettingDeadlineMinutes: 5,
            AllowedCompetitionIds: null);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task Handle_InvalidCompetitionIds_ReturnsFailure()
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

        // Empty competitions list - no valid competitions
        var mockCompetitions = CreateMockDbSet(new List<Domain.Entities.Competition>().AsQueryable());
        Context.Competitions.Returns(mockCompetitions);

        var invalidCompetitionId = Guid.NewGuid();
        var command = new UpdateLeagueCommand(
            LeagueId: leagueId,
            Name: "New Name",
            Description: null,
            IsPublic: false,
            MaxMembers: 20,
            ScoreExactMatch: 3,
            ScoreCorrectResult: 1,
            BettingDeadlineMinutes: 5,
            AllowedCompetitionIds: new[] { invalidCompetitionId });

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }
}
