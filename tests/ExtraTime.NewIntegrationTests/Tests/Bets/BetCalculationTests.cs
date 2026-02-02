using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bets;
using ExtraTime.Application.Features.Bets.Commands.CalculateBetResults;
using ExtraTime.Application.Features.Bets.Commands.RecalculateLeagueStandings;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.NewIntegrationTests.Base;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.NewIntegrationTests.Tests.Bets;

public sealed class BetCalculationTests : NewIntegrationTestBase
{
    [Test]
    public async Task CalculateBetResults_ValidMatch_UpdatesBetResults()
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

        var competition = new CompetitionBuilder().Build();
        Context.Competitions.Add(competition);

        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Teams.AddRange(homeTeam, awayTeam);

        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .WithScore(2, 1)
            .WithStatus(MatchStatus.Finished)
            .Build();
        Context.Matches.Add(match);

        var bet = new BetBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(userId)
            .WithMatchId(match.Id)
            .WithPrediction(2, 1) // Exact match
            .Build();
        Context.Bets.Add(bet);

        await Context.SaveChangesAsync();

        var jobDispatcher = Substitute.For<IJobDispatcher>();
        var handler = new CalculateBetResultsCommandHandler(Context, jobDispatcher);

        var command = new CalculateBetResultsCommand(match.Id, competition.Id);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        
        var betResult = await Context.BetResults
            .FirstOrDefaultAsync(r => r.BetId == bet.Id);
        
        await Assert.That(betResult).IsNotNull();
        await Assert.That(betResult!.PointsEarned).IsEqualTo(3);
        await Assert.That(betResult.IsExactMatch).IsTrue();

        await jobDispatcher.Received(1).EnqueueAsync(
            "RecalculateLeagueStandings", 
            Arg.Any<object>(), 
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CalculateBetResults_NoBets_ReturnsSuccessWithNoChanges()
    {
        // Arrange
        var competition = new CompetitionBuilder().Build();
        Context.Competitions.Add(competition);

        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Teams.AddRange(homeTeam, awayTeam);

        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .WithScore(2, 1)
            .WithStatus(MatchStatus.Finished)
            .Build();
        Context.Matches.Add(match);

        await Context.SaveChangesAsync();

        var jobDispatcher = Substitute.For<IJobDispatcher>();
        var handler = new CalculateBetResultsCommandHandler(Context, jobDispatcher);
        var command = new CalculateBetResultsCommand(match.Id, competition.Id);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();

        var betResultsCount = await Context.BetResults.CountAsync();
        await Assert.That(betResultsCount).IsEqualTo(0);

        await jobDispatcher.DidNotReceive().EnqueueAsync(
            Arg.Any<string>(),
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CalculateBetResults_MatchNotFound_ReturnsFailure()
    {
        // Arrange
        var nonExistentMatchId = Guid.NewGuid();
        var competition = new CompetitionBuilder().Build();
        Context.Competitions.Add(competition);
        await Context.SaveChangesAsync();

        var jobDispatcher = Substitute.For<IJobDispatcher>();
        var handler = new CalculateBetResultsCommandHandler(Context, jobDispatcher);
        var command = new CalculateBetResultsCommand(nonExistentMatchId, competition.Id);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo(BetErrors.MatchNotFound);
    }

    [Test]
    public async Task CalculateBetResults_NoFinalScores_ReturnsFailure()
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

        var competition = new CompetitionBuilder().Build();
        Context.Competitions.Add(competition);

        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Teams.AddRange(homeTeam, awayTeam);

        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .WithStatus(MatchStatus.Scheduled)
            .Build();
        Context.Matches.Add(match);

        var bet = Bet.Place(league.Id, userId, match.Id, 2, 1);
        Context.Bets.Add(bet);

        await Context.SaveChangesAsync();

        var jobDispatcher = Substitute.For<IJobDispatcher>();
        var handler = new CalculateBetResultsCommandHandler(Context, jobDispatcher);
        var command = new CalculateBetResultsCommand(match.Id, competition.Id);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task CalculateBetResults_ExistingResults_UpdatesResults()
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

        var competition = new CompetitionBuilder().Build();
        Context.Competitions.Add(competition);

        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Teams.AddRange(homeTeam, awayTeam);

        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .WithScore(2, 1)
            .WithStatus(MatchStatus.Finished)
            .Build();
        Context.Matches.Add(match);

        var bet = Bet.Place(league.Id, userId, match.Id, 2, 1);
        Context.Bets.Add(bet);
        await Context.SaveChangesAsync();

        var existingResult = BetResult.Create(bet.Id, 0, false, false);
        Context.BetResults.Add(existingResult);
        await Context.SaveChangesAsync();

        var jobDispatcher = Substitute.For<IJobDispatcher>();
        var handler = new CalculateBetResultsCommandHandler(Context, jobDispatcher);
        var command = new CalculateBetResultsCommand(match.Id, competition.Id);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();

        var updatedResult = await Context.BetResults
            .FirstOrDefaultAsync(r => r.BetId == bet.Id);

        await Assert.That(updatedResult).IsNotNull();
        await Assert.That(updatedResult!.PointsEarned).IsEqualTo(3);
        await Assert.That(updatedResult.IsExactMatch).IsTrue();
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
            .Build();
        var league2 = new LeagueBuilder()
            .WithOwnerId(userId2)
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

    [Test]
    public async Task CalculateBetResults_ExistingMatchWithScore_CalculatesAndCreatesResults()
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

        var competition = new CompetitionBuilder().Build();
        Context.Competitions.Add(competition);

        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Teams.AddRange(homeTeam, awayTeam);

        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .WithScore(2, 1)
            .WithStatus(MatchStatus.Finished)
            .Build();
        Context.Matches.Add(match);

        var bet = Bet.Place(league.Id, userId, match.Id, 2, 1);
        Context.Bets.Add(bet);

        await Context.SaveChangesAsync();

        var jobDispatcher = Substitute.For<IJobDispatcher>();
        var handler = new CalculateBetResultsCommandHandler(Context, jobDispatcher);
        var command = new CalculateBetResultsCommand(match.Id, competition.Id);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();

        var betResult = await Context.BetResults
            .FirstOrDefaultAsync(r => r.BetId == bet.Id);

        await Assert.That(betResult).IsNotNull();
        await Assert.That(betResult!.PointsEarned).IsEqualTo(3);
        await Assert.That(betResult.IsExactMatch).IsTrue();
        await Assert.That(betResult.IsCorrectResult).IsTrue();
    }

    [Test]
    public async Task CalculateBetResults_EnqueuesStandingsRecalculationJob()
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

        var competition = new CompetitionBuilder().Build();
        Context.Competitions.Add(competition);

        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Teams.AddRange(homeTeam, awayTeam);

        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .WithScore(2, 1)
            .WithStatus(MatchStatus.Finished)
            .Build();
        Context.Matches.Add(match);

        var bet = Bet.Place(league.Id, userId, match.Id, 2, 1);
        Context.Bets.Add(bet);

        await Context.SaveChangesAsync();

        var jobDispatcher = Substitute.For<IJobDispatcher>();
        var handler = new CalculateBetResultsCommandHandler(Context, jobDispatcher);
        var command = new CalculateBetResultsCommand(match.Id, competition.Id);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();

        await jobDispatcher.Received(1).EnqueueAsync(
            "RecalculateLeagueStandings",
            Arg.Is<object>(o => o.GetType().GetProperty("leagueIds") != null),
            Arg.Any<CancellationToken>());
    }

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
}
