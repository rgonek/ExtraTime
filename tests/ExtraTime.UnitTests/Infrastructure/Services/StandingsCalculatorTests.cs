using ExtraTime.Domain.Entities;
using ExtraTime.Infrastructure.Services;
using ExtraTime.UnitTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.UnitTests.Infrastructure.Services;

public sealed class StandingsCalculatorTests : HandlerTestBase
{
    private readonly StandingsCalculator _calculator;

    public StandingsCalculatorTests()
    {
        _calculator = new StandingsCalculator(Context);
    }

    [Test]
    public async Task RecalculateLeagueStandingsAsync_CalculatesCorrectTotals()
    {
        // Arrange
        var leagueId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var match1 = new MatchBuilder().WithMatchDate(DateTime.UtcNow.AddHours(-2)).Build();
        var match2 = new MatchBuilder().WithMatchDate(DateTime.UtcNow.AddHours(-1)).Build();

        var bet1 = new BetBuilder()
            .WithLeagueId(leagueId)
            .WithUserId(userId)
            .WithMatchId(match1.Id)
            .Build();
        bet1.Match = match1;
        bet1.Result = new BetResult { PointsEarned = 3, IsExactMatch = true, IsCorrectResult = true };

        var bet2 = new BetBuilder()
            .WithLeagueId(leagueId)
            .WithUserId(userId)
            .WithMatchId(match2.Id)
            .Build();
        bet2.Match = match2;
        bet2.Result = new BetResult { PointsEarned = 1, IsExactMatch = false, IsCorrectResult = true };

        var bets = new List<Bet> { bet1, bet2 }.AsQueryable();
        var mockBets = CreateMockDbSet(bets);
        Context.Bets.Returns(mockBets);
        
        var mockStandings = CreateMockDbSet(new List<LeagueStanding>().AsQueryable());
        Context.LeagueStandings.Returns(mockStandings);

        // Act
        await _calculator.RecalculateLeagueStandingsAsync(leagueId);

        // Assert
        Context.LeagueStandings.Received(1).Add(Arg.Is<LeagueStanding>(s =>
            s.UserId == userId &&
            s.TotalPoints == 4 &&
            s.BetsPlaced == 2 &&
            s.ExactMatches == 1 &&
            s.CorrectResults == 2));
        await Context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RecalculateLeagueStandingsAsync_CalculatesCorrectStreaks()
    {
        // Arrange
        var leagueId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // 5 bets: Correct, Correct, Wrong, Correct, Correct (Current streak 2, Best streak 2)
        var results = new[] { true, true, false, true, true };
        var bets = new List<Bet>();

        for (int i = 0; i < results.Length; i++)
        {
            var match = new MatchBuilder().WithMatchDate(DateTime.UtcNow.AddHours(-10 + i)).Build();
            var bet = new BetBuilder()
                .WithLeagueId(leagueId)
                .WithUserId(userId)
                .WithMatchId(match.Id)
                .Build();
            bet.Match = match;
            bet.Result = new BetResult { IsCorrectResult = results[i], PointsEarned = results[i] ? 1 : 0 };
            bets.Add(bet);
        }

        var mockBets = CreateMockDbSet(bets.AsQueryable());
        Context.Bets.Returns(mockBets);
        
        var mockStandings = CreateMockDbSet(new List<LeagueStanding>().AsQueryable());
        Context.LeagueStandings.Returns(mockStandings);

        // Act
        await _calculator.RecalculateLeagueStandingsAsync(leagueId);

        // Assert
        Context.LeagueStandings.Received(1).Add(Arg.Is<LeagueStanding>(s =>
            s.CurrentStreak == 2 &&
            s.BestStreak == 2));
    }

    [Test]
    public async Task RecalculateLeagueStandingsAsync_UpdatesExistingStanding()
    {
        // Arrange
        var leagueId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var match = new MatchBuilder().WithScore(1, 1).Build();
        var bet = new BetBuilder().WithLeagueId(leagueId).WithUserId(userId).WithMatchId(match.Id).Build();
        bet.Match = match;
        bet.Result = new BetResult { PointsEarned = 3, IsExactMatch = true, IsCorrectResult = true };

        var existingStanding = LeagueStanding.Create(leagueId, userId);
        existingStanding.TotalPoints = 0;

        var mockBets = CreateMockDbSet(new List<Bet> { bet }.AsQueryable());
        Context.Bets.Returns(mockBets);
        
        var mockStandings = CreateMockDbSet(new List<LeagueStanding> { existingStanding }.AsQueryable());
        Context.LeagueStandings.Returns(mockStandings);

        // Act
        await _calculator.RecalculateLeagueStandingsAsync(leagueId);

        // Assert
        await Assert.That(existingStanding.TotalPoints).IsEqualTo(3);
        Context.LeagueStandings.DidNotReceive().Add(Arg.Any<LeagueStanding>());
        await Context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
