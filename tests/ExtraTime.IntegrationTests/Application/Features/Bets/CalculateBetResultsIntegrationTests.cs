using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bets.Commands.CalculateBetResults;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.Infrastructure.Services;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.IntegrationTests.Application.Features.Bets;

public sealed class CalculateBetResultsIntegrationTests : IntegrationTestBase
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
}
