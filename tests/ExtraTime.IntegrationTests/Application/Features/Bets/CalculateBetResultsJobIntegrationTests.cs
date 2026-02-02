using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bets;
using ExtraTime.Application.Features.Bets.Commands.CalculateBetResults;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.IntegrationTests.Attributes;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.IntegrationTests.Application.Features.Bets;

[TestCategory(TestCategories.RequiresDatabase)]
public sealed class CalculateBetResultsJobIntegrationTests : IntegrationTestBase
{
    protected override bool UseSqlDatabase => true;

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

        var bet = new BetBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(userId)
            .WithMatchId(match.Id)
            .WithPrediction(2, 1)
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
        await Assert.That(betResult.IsCorrectResult).IsTrue();
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

        var bet = new BetBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(userId)
            .WithMatchId(match.Id)
            .WithPrediction(2, 1)
            .Build();
        Context.Bets.Add(bet);

        await Context.SaveChangesAsync();

        var jobDispatcher = Substitute.For<IJobDispatcher>();
        var handler = new CalculateBetResultsCommandHandler(Context, jobDispatcher);
        var command = new CalculateBetResultsCommand(match.Id, competition.Id);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo("Match does not have final scores yet");
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

        var bet = new BetBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(userId)
            .WithMatchId(match.Id)
            .WithPrediction(2, 1)
            .Build();
        Context.Bets.Add(bet);
        await Context.SaveChangesAsync();

        var existingResult = new BetResultBuilder()
            .WithBetId(bet.Id)
            .WithPointsEarned(0)
            .WithIsExactMatch(false)
            .WithIsCorrectResult(false)
            .WithCalculatedAt(DateTime.UtcNow.AddHours(-2))
            .Build();
        Context.BetResults.Add(existingResult);
        await Context.SaveChangesAsync();

        // Small delay to ensure the new CalculatedAt will be different
        await Task.Delay(50);

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
        await Assert.That(updatedResult.IsCorrectResult).IsTrue();
        await Assert.That(updatedResult.CalculatedAt).IsGreaterThanOrEqualTo(existingResult.CalculatedAt);
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

        var bet = new BetBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(userId)
            .WithMatchId(match.Id)
            .WithPrediction(2, 1)
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

        await jobDispatcher.Received(1).EnqueueAsync(
            "RecalculateLeagueStandings",
            Arg.Is<object>(o => o.GetType().GetProperty("leagueIds") != null),
            Arg.Any<CancellationToken>());
    }
}
