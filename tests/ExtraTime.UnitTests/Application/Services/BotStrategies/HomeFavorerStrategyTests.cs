using ExtraTime.Application.Features.Bots.Strategies;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;

namespace ExtraTime.UnitTests.Application.Services.BotStrategies;

public sealed class HomeFavorerStrategyTests
{
    private readonly HomeFavorerStrategy _strategy = new();

    [Test]
    public async Task GeneratePrediction_HomeTeamFavored_ReturnsHigherHomeScore()
    {
        // Arrange
        var match = CreateTestMatch();

        // Act
        var (homeScore, awayScore) = _strategy.GeneratePrediction(match, null);

        // Assert - Home should be greater than away or at least equal
        await Assert.That(homeScore).IsGreaterThanOrEqualTo(awayScore);
    }

    [Test]
    public async Task GeneratePrediction_HomeScore_IsAtLeast1()
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

        // Assert - Home score should always be at least 1
        await Assert.That(homeScores.Min()).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task GeneratePrediction_AwayScore_IsLessThanHome()
    {
        // Arrange
        var match = CreateTestMatch();

        // Act - Generate multiple predictions to verify pattern
        var predictions = new List<(int Home, int Away)>();
        for (int i = 0; i < 50; i++)
        {
            predictions.Add(_strategy.GeneratePrediction(match, null));
        }

        // Assert - Away score should always be less than home score
        foreach (var (home, away) in predictions)
        {
            await Assert.That(away).IsLessThan(home);
        }
    }

    [Test]
    public async Task GeneratePrediction_HomeScoreRange_Is1To3()
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
        await Assert.That(homeScores.Min()).IsGreaterThanOrEqualTo(1);
        await Assert.That(homeScores.Max()).IsLessThanOrEqualTo(3);
    }

    [Test]
    public async Task GeneratePrediction_AwayScoreRange_Is0To2()
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
        await Assert.That(awayScores.Min()).IsGreaterThanOrEqualTo(0);
        await Assert.That(awayScores.Max()).IsLessThanOrEqualTo(2);
    }

    [Test]
    public async Task StrategyType_ReturnsHomeFavorer()
    {
        // Assert
        await Assert.That(_strategy.StrategyType).IsEqualTo(BotStrategy.HomeFavorer);
    }

    [Test]
    public async Task GeneratePrediction_WithConfiguration_IgnoresConfiguration()
    {
        // Arrange
        var match = CreateTestMatch();
        var config = "{\"test\": true}";

        // Act
        var (homeScore, awayScore) = _strategy.GeneratePrediction(match, config);

        // Assert - Should still follow home favorer pattern
        await Assert.That(homeScore).IsGreaterThanOrEqualTo(awayScore);
        await Assert.That(homeScore).IsGreaterThanOrEqualTo(1);
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
