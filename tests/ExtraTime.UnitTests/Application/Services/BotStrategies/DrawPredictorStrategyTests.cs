using ExtraTime.Application.Features.Bots.Strategies;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;

namespace ExtraTime.UnitTests.Application.Services.BotStrategies;

public sealed class DrawPredictorStrategyTests
{
    private readonly DrawPredictorStrategy _strategy = new();

    [Test]
    public async Task GeneratePrediction_CloselyMatchedTeams_ReturnsEqualOrNearEqual()
    {
        // Arrange
        var match = CreateTestMatch();

        // Act - Generate multiple predictions
        var predictions = new List<(int Home, int Away)>();
        for (int i = 0; i < 100; i++)
        {
            predictions.Add(_strategy.GeneratePrediction(match, null));
        }

        // Assert - Should have some draws (equal scores)
        var draws = predictions.Count(p => p.Home == p.Away);
        await Assert.That(draws).IsGreaterThan(0);
    }

    [Test]
    public async Task GeneratePrediction_Draws_AreCommon()
    {
        // Arrange
        var match = CreateTestMatch();

        // Act - Generate many predictions
        var predictions = new List<(int Home, int Away)>();
        for (int i = 0; i < 100; i++)
        {
            predictions.Add(_strategy.GeneratePrediction(match, null));
        }

        // Assert - Approximately 70% should be draws
        var draws = predictions.Count(p => p.Home == p.Away);
        var drawRate = (double)draws / predictions.Count;
        await Assert.That(drawRate).IsGreaterThan(0.5); // At least 50% draws
    }

    [Test]
    public async Task GeneratePrediction_WhenNotDraw_ScoreDifference_IsMinimal()
    {
        // Arrange
        var match = CreateTestMatch();

        // Act
        var nonDrawPredictions = new List<(int Home, int Away)>();
        for (int i = 0; i < 100; i++)
        {
            var prediction = _strategy.GeneratePrediction(match, null);
            if (prediction.HomeScore != prediction.AwayScore)
            {
                nonDrawPredictions.Add(prediction);
            }
        }

        // Assert - Non-draws should have minimal score difference (1 goal)
        foreach (var (home, away) in nonDrawPredictions)
        {
            var diff = Math.Abs(home - away);
            await Assert.That(diff).IsEqualTo(1);
        }
    }

    [Test]
    public async Task GeneratePrediction_ScoreRange_Is0To2()
    {
        // Arrange
        var match = CreateTestMatch();
        var scores = new List<int>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            var (home, away) = _strategy.GeneratePrediction(match, null);
            scores.Add(home);
            scores.Add(away);
        }

        // Assert
        await Assert.That(scores.Min()).IsGreaterThanOrEqualTo(0);
        await Assert.That(scores.Max()).IsLessThanOrEqualTo(2);
    }

    [Test]
    public async Task StrategyType_ReturnsDrawPredictor()
    {
        // Assert
        await Assert.That(_strategy.StrategyType).IsEqualTo(BotStrategy.DrawPredictor);
    }

    [Test]
    public async Task GeneratePrediction_WithConfiguration_IgnoresConfiguration()
    {
        // Arrange
        var match = CreateTestMatch();
        var config = "{\"test\": true}";

        // Act
        var (homeScore, awayScore) = _strategy.GeneratePrediction(match, config);

        // Assert - Should still return valid scores in expected range
        await Assert.That(homeScore).IsGreaterThanOrEqualTo(0);
        await Assert.That(homeScore).IsLessThanOrEqualTo(2);
        await Assert.That(awayScore).IsGreaterThanOrEqualTo(0);
        await Assert.That(awayScore).IsLessThanOrEqualTo(2);
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
