using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bets.Commands.RecalculateLeagueStandings;
using ExtraTime.Domain.Entities;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.TestData;
using NSubstitute;

namespace ExtraTime.IntegrationTests.Application.Features.Bets;

public sealed class RecalculateStandingsJobIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task RecalculateStandings_ValidLeague_RecalculatesSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        Context.Users.Add(user);

        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .WithScoringRules(3, 1)
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        var standingsCalculator = Substitute.For<IStandingsCalculator>();
        var handler = new RecalculateLeagueStandingsCommandHandler(standingsCalculator);
        var command = new RecalculateLeagueStandingsCommand(new[] { league.Id });

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await standingsCalculator.Received(1).RecalculateLeagueStandingsAsync(
            league.Id,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RecalculateStandings_MultipleLeagues_ProcessesAll()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var user1 = new UserBuilder().WithId(userId1).Build();
        var user2 = new UserBuilder().WithId(userId2).Build();
        Context.Users.AddRange(user1, user2);

        var league1 = new LeagueBuilder()
            .WithOwnerId(userId1)
            .WithScoringRules(3, 1)
            .Build();
        var league2 = new LeagueBuilder()
            .WithOwnerId(userId2)
            .WithScoringRules(5, 2)
            .Build();
        Context.Leagues.AddRange(league1, league2);
        await Context.SaveChangesAsync();

        var standingsCalculator = Substitute.For<IStandingsCalculator>();
        var handler = new RecalculateLeagueStandingsCommandHandler(standingsCalculator);
        var command = new RecalculateLeagueStandingsCommand(new[] { league1.Id, league2.Id });

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await standingsCalculator.Received(1).RecalculateLeagueStandingsAsync(
            league1.Id,
            Arg.Any<CancellationToken>());
        await standingsCalculator.Received(1).RecalculateLeagueStandingsAsync(
            league2.Id,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RecalculateStandings_NoLeagues_ReturnsSuccess()
    {
        // Arrange
        var standingsCalculator = Substitute.For<IStandingsCalculator>();
        var handler = new RecalculateLeagueStandingsCommandHandler(standingsCalculator);
        var command = new RecalculateLeagueStandingsCommand(Array.Empty<Guid>());

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await standingsCalculator.DidNotReceive().RecalculateLeagueStandingsAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }
}
