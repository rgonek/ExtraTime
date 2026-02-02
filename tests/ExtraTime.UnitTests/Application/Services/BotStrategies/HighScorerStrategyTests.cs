using ExtraTime.Application.Features.Bots.Strategies;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;

namespace ExtraTime.UnitTests.Application.Services.BotStrategies;

public sealed class HighScorerStrategyTests
{
    private readonly HighScorerStrategy _strategy = new();

    [Test]
    public async Task GeneratePrediction_ReturnsHighTotalGoals()
    {
        // Arrange
        var match = CreateTestMatch();

        // Act
        var (homeScore, awayScore) = _strategy.GeneratePrediction(match, null);

        // Assert - Total should be at least 3
        var totalGoals = homeScore + awayScore;
        await Assert.That(totalGoals).IsGreaterThanOrEqualTo(3);
    }

    [Test]
    public async Task GeneratePrediction_HomeScore_IsAtLeast2()
    {
        // Arrange
        var match = CreateTestMatch();

        // Act - Generate multiple predictions
        var homeScores = new List<int>();
        for (int i = 0; i < 50; i++)
        {
            var (homeScore, _) = _strategy.GeneratePrediction(match, null);
            homeScores.Add(homeScore);
        }

        // Assert
        await Assert.That(homeScores.Min()).IsGreaterThanOrEqualTo(2);
    }

    [Test]
    public async Task GeneratePrediction_AwayScore_IsAtLeast1()
    {
        // Arrange
        var match = CreateTestMatch();

        // Act - Generate multiple predictions
        var awayScores = new List<int>();
        for (int i = 0; i < 50; i++)
        {
            var (_, awayScore) = _strategy.GeneratePrediction(match, null);
            awayScores.Add(awayScore);
        }

        // Assert
        await Assert.That(awayScores.Min()).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task GeneratePrediction_HomeScoreRange_Is2To4()
    {
        // Arrange
        var match = CreateTestMatch();
        var homeScores = new List<int>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            var (homeScore, _) = _strategy.GeneratePrediction(match, null);
            homeScores.Add(homeScore);
        }

        // Assert
        await Assert.That(homeScores.Min()).IsGreaterThanOrEqualTo(2);
        await Assert.That(homeScores.Max()).IsLessThanOrEqualTo(4);
    }

    [Test]
    public async Task GeneratePrediction_AwayScoreRange_Is1To3()
    {
        // Arrange
        var match = CreateTestMatch();
        var awayScores = new List<int>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            var (_, awayScore) = _strategy.GeneratePrediction(match, null);
            awayScores.Add(awayScore);
        }

        // Assert
        await Assert.That(awayScores.Min()).IsGreaterThanOrEqualTo(1);
        await Assert.That(awayScores.Max()).IsLessThanOrEqualTo(3);
    }

    [Test]
    public async Task GeneratePrediction_TotalGoals_UsuallyAbove2_5()
    {
        // Arrange
        var match = CreateTestMatch();

        // Act
        var totalGoals = new List<int>();
        for (int i = 0; i < 100; i++)
        {
            var (home, away) = _strategy.GeneratePrediction(match, null);
            totalGoals.Add(home + away);
        }

        // Assert - Most predictions should be 3+ goals (over 2.5)
        var over2_5 = totalGoals.Count(g => g > 2);
        var rate = (double)over2_5 / totalGoals.Count;
        await Assert.That(rate).IsGreaterThan(0.9); // At least 90% over 2.5
    }

    [Test]
    public async Task StrategyType_ReturnsHighScorer()
    {
        // Assert
        await Assert.That(_strategy.StrategyType).IsEqualTo(BotStrategy.HighScorer);
    }

    [Test]
    public async Task GeneratePrediction_WithConfiguration_IgnoresConfiguration()
    {
        // Arrange
        var match = CreateTestMatch();
        var config = "{\"test\": true}";

        // Act
        var (homeScore, awayScore) = _strategy.GeneratePrediction(match, config);

        // Assert - Should still return high scores
        await Assert.That(homeScore).IsGreaterThanOrEqualTo(2);
        await Assert.That(awayScore).IsGreaterThanOrEqualTo(1);
    }

    private static Match CreateTestMatch(int externalId = 12345)
    {
        return Match.Create(
            externalId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1),
            MatchStatus.Scheduled);
    }
}
