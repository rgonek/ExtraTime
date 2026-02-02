using ExtraTime.Application.Features.Bots.Strategies;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;

namespace ExtraTime.UnitTests.Application.Services.BotStrategies;

public sealed class RandomStrategyTests
{
    private readonly RandomStrategy _strategy = new();

    [Test]
    public async Task GeneratePrediction_ReturnsValidScore()
    {
        // Arrange
        var match = CreateTestMatch();

        // Act
        var (homeScore, awayScore) = _strategy.GeneratePrediction(match, null);

        // Assert
        await Assert.That(homeScore).IsGreaterThanOrEqualTo(0);
        await Assert.That(homeScore).IsLessThan(5);
        await Assert.That(awayScore).IsGreaterThanOrEqualTo(0);
        await Assert.That(awayScore).IsLessThan(4);
    }

    [Test]
    public async Task GeneratePrediction_MultipleCalls_ReturnsDifferentScores()
    {
        // Arrange
        var match = CreateTestMatch();
        var predictions = new List<(int Home, int Away)>();

        // Act
        for (int i = 0; i < 20; i++)
        {
            predictions.Add(_strategy.GeneratePrediction(match, null));
        }

        // Assert - Check that we get some variety (not all the same)
        var uniquePredictions = predictions.Distinct().ToList();
        await Assert.That(uniquePredictions.Count).IsGreaterThan(1);
    }

    [Test]
    public async Task GeneratePrediction_WithAnyMatch_ReturnsConsistentFormat()
    {
        // Arrange
        var matches = new[]
        {
            CreateTestMatch(1),
            CreateTestMatch(2),
            CreateTestMatch(3),
        };

        // Act & Assert
        foreach (var match in matches)
        {
            var (homeScore, awayScore) = _strategy.GeneratePrediction(match, null);
            await Assert.That(homeScore).IsGreaterThanOrEqualTo(0);
            await Assert.That(awayScore).IsGreaterThanOrEqualTo(0);
        }
    }

    [Test]
    public async Task StrategyType_ReturnsRandom()
    {
        // Assert
        await Assert.That(_strategy.StrategyType).IsEqualTo(BotStrategy.Random);
    }

    [Test]
    public async Task GeneratePrediction_WithConfiguration_IgnoresConfiguration()
    {
        // Arrange
        var match = CreateTestMatch();
        var config = "{\"test\": true}";

        // Act
        var (homeScore, awayScore) = _strategy.GeneratePrediction(match, config);

        // Assert - Should still return valid scores
        await Assert.That(homeScore).IsGreaterThanOrEqualTo(0);
        await Assert.That(awayScore).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task GeneratePrediction_HomeScoreRange_Is0To4()
    {
        // Arrange
        var match = CreateTestMatch();
        var homeScores = new List<int>();

        // Act - Generate many predictions to test range
        for (int i = 0; i < 100; i++)
        {
            var (homeScore, _) = _strategy.GeneratePrediction(match, null);
            homeScores.Add(homeScore);
        }

        // Assert
        await Assert.That(homeScores.Min()).IsGreaterThanOrEqualTo(0);
        await Assert.That(homeScores.Max()).IsLessThan(5);
    }

    [Test]
    public async Task GeneratePrediction_AwayScoreRange_Is0To3()
    {
        // Arrange
        var match = CreateTestMatch();
        var awayScores = new List<int>();

        // Act - Generate many predictions to test range
        for (int i = 0; i < 100; i++)
        {
            var (_, awayScore) = _strategy.GeneratePrediction(match, null);
            awayScores.Add(awayScore);
        }

        // Assert
        await Assert.That(awayScores.Min()).IsGreaterThanOrEqualTo(0);
        await Assert.That(awayScores.Max()).IsLessThan(4);
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
