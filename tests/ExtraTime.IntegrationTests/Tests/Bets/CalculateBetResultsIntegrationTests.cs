using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bets;
using ExtraTime.Application.Features.Bets.Commands.CalculateBetResults;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.IntegrationTests.Attributes;
using ExtraTime.IntegrationTests.Base;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.IntegrationTests.Tests.Bets;

public sealed class CalculateBetResultsIntegrationTests : IntegrationTestBase
{
    [Test]
    [TestCategory(TestCategories.Significant)]
    [SkipOnGitHubActions]
    public async Task Calculate_ExactMatch_AwardsExactPoints()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        Context.Users.Add(user);

        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .WithScoringRules(5, 2)
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
            .WithScore(3, 2) // Final score 3-2
            .WithStatus(MatchStatus.Finished)
            .Build();
        Context.Matches.Add(match);

        var bet = new BetBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(userId)
            .WithMatchId(match.Id)
            .WithPrediction(3, 2) // Exact match prediction
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
        await Assert.That(betResult!.PointsEarned).IsEqualTo(5); // Exact match points
        await Assert.That(betResult.IsExactMatch).IsTrue();
        await Assert.That(betResult.IsCorrectResult).IsTrue();
    }

    [Test]
    [TestCategory(TestCategories.Significant)]
    [SkipOnGitHubActions]
    public async Task Calculate_CorrectResult_AwardsResultPoints()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        Context.Users.Add(user);

        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .WithScoringRules(5, 2)
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
            .WithScore(3, 1) // Final score 3-1 (home win)
            .WithStatus(MatchStatus.Finished)
            .Build();
        Context.Matches.Add(match);

        var bet = new BetBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(userId)
            .WithMatchId(match.Id)
            .WithPrediction(2, 0) // Predicted home win (correct result, not exact)
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
        await Assert.That(betResult!.PointsEarned).IsEqualTo(2); // Correct result points only
        await Assert.That(betResult.IsExactMatch).IsFalse();
        await Assert.That(betResult.IsCorrectResult).IsTrue();
    }

    [Test]
    [TestCategory(TestCategories.Significant)]
    [SkipOnGitHubActions]
    public async Task Calculate_WrongResult_ZeroPoints()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        Context.Users.Add(user);

        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .WithScoringRules(5, 2)
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
            .WithScore(1, 3) // Final score 1-3 (away win)
            .WithStatus(MatchStatus.Finished)
            .Build();
        Context.Matches.Add(match);

        var bet = new BetBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(userId)
            .WithMatchId(match.Id)
            .WithPrediction(2, 1) // Predicted home win (wrong result)
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
        await Assert.That(betResult!.PointsEarned).IsEqualTo(0); // No points for wrong prediction
        await Assert.That(betResult.IsExactMatch).IsFalse();
        await Assert.That(betResult.IsCorrectResult).IsFalse();
    }

    [Test]
    [TestCategory(TestCategories.Significant)]
    [SkipOnGitHubActions]
    public async Task Calculate_CustomScoring_UsesLeagueRules()
    {
        // Arrange - Create two leagues with different scoring rules
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var user1 = new UserBuilder().WithId(userId1).Build();
        var user2 = new UserBuilder().WithId(userId2).Build();
        Context.Users.AddRange(user1, user2);

        var league1 = new LeagueBuilder()
            .WithOwnerId(userId1)
            .WithScoringRules(10, 5) // High scoring league
            .Build();
        var league2 = new LeagueBuilder()
            .WithOwnerId(userId2)
            .WithScoringRules(2, 1) // Low scoring league
            .Build();
        Context.Leagues.AddRange(league1, league2);

        var competition = new CompetitionBuilder().Build();
        Context.Competitions.Add(competition);

        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Teams.AddRange(homeTeam, awayTeam);

        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .WithScore(2, 2) // Draw
            .WithStatus(MatchStatus.Finished)
            .Build();
        Context.Matches.Add(match);

        // Both users predict exact draw
        var bet1 = new BetBuilder()
            .WithLeagueId(league1.Id)
            .WithUserId(userId1)
            .WithMatchId(match.Id)
            .WithPrediction(2, 2)
            .Build();
        var bet2 = new BetBuilder()
            .WithLeagueId(league2.Id)
            .WithUserId(userId2)
            .WithMatchId(match.Id)
            .WithPrediction(2, 2)
            .Build();
        Context.Bets.AddRange(bet1, bet2);

        await Context.SaveChangesAsync();

        var jobDispatcher = Substitute.For<IJobDispatcher>();
        var handler = new CalculateBetResultsCommandHandler(Context, jobDispatcher);

        var command = new CalculateBetResultsCommand(match.Id, competition.Id);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();

        var betResult1 = await Context.BetResults
            .FirstOrDefaultAsync(r => r.BetId == bet1.Id);
        var betResult2 = await Context.BetResults
            .FirstOrDefaultAsync(r => r.BetId == bet2.Id);

        await Assert.That(betResult1).IsNotNull();
        await Assert.That(betResult2).IsNotNull();

        // League 1 gets 10 points for exact match
        await Assert.That(betResult1!.PointsEarned).IsEqualTo(10);
        // League 2 gets only 2 points for exact match
        await Assert.That(betResult2!.PointsEarned).IsEqualTo(2);
    }

    [Test]
    [TestCategory(TestCategories.Significant)]
    [SkipOnGitHubActions]
    public async Task Calculate_MultipleBets_AllProcessed()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var userId3 = Guid.NewGuid();
        var user1 = new UserBuilder().WithId(userId1).Build();
        var user2 = new UserBuilder().WithId(userId2).Build();
        var user3 = new UserBuilder().WithId(userId3).Build();
        Context.Users.AddRange(user1, user2, user3);

        var league = new LeagueBuilder()
            .WithOwnerId(userId1)
            .WithScoringRules(5, 2)
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
            .WithScore(2, 1) // Home win
            .WithStatus(MatchStatus.Finished)
            .Build();
        Context.Matches.Add(match);

        // Create multiple bets with different outcomes
        var exactBet = new BetBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(userId1)
            .WithMatchId(match.Id)
            .WithPrediction(2, 1) // Exact match
            .Build();

        var correctResultBet = new BetBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(userId2)
            .WithMatchId(match.Id)
            .WithPrediction(3, 0) // Home win (correct result)
            .Build();

        var wrongBet = new BetBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(userId3)
            .WithMatchId(match.Id)
            .WithPrediction(1, 2) // Away win (wrong)
            .Build();

        Context.Bets.AddRange(exactBet, correctResultBet, wrongBet);
        await Context.SaveChangesAsync();

        var jobDispatcher = Substitute.For<IJobDispatcher>();
        var handler = new CalculateBetResultsCommandHandler(Context, jobDispatcher);

        var command = new CalculateBetResultsCommand(match.Id, competition.Id);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();

        var exactResult = await Context.BetResults
            .FirstOrDefaultAsync(r => r.BetId == exactBet.Id);
        var correctResult = await Context.BetResults
            .FirstOrDefaultAsync(r => r.BetId == correctResultBet.Id);
        var wrongResult = await Context.BetResults
            .FirstOrDefaultAsync(r => r.BetId == wrongBet.Id);

        // All results should be created
        await Assert.That(exactResult).IsNotNull();
        await Assert.That(correctResult).IsNotNull();
        await Assert.That(wrongResult).IsNotNull();

        // Verify points
        await Assert.That(exactResult!.PointsEarned).IsEqualTo(5);
        await Assert.That(exactResult.IsExactMatch).IsTrue();

        await Assert.That(correctResult!.PointsEarned).IsEqualTo(2);
        await Assert.That(correctResult.IsExactMatch).IsFalse();
        await Assert.That(correctResult.IsCorrectResult).IsTrue();

        await Assert.That(wrongResult!.PointsEarned).IsEqualTo(0);
        await Assert.That(wrongResult.IsExactMatch).IsFalse();
        await Assert.That(wrongResult.IsCorrectResult).IsFalse();

        // Verify all 3 bet results were created
        var totalResults = await Context.BetResults.CountAsync();
        await Assert.That(totalResults).IsEqualTo(3);

        // Verify job was enqueued with all league IDs (just one league in this case)
        await jobDispatcher.Received(1).EnqueueAsync(
            "RecalculateLeagueStandings",
            Arg.Is<object>(o => o.GetType().GetProperty("leagueIds") != null),
            Arg.Any<CancellationToken>());
    }
}
