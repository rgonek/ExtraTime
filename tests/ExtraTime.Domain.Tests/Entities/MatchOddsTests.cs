using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;

namespace ExtraTime.Domain.Tests.Entities;

public sealed class MatchOddsTests
{
    [Test]
    public async Task CalculateProbabilities_WithHomeFavorite_ShouldNormalizeAndSetFavorite()
    {
        // Arrange
        var odds = new MatchOdds
        {
            MatchId = Guid.NewGuid(),
            HomeWinOdds = 1.8,
            DrawOdds = 3.6,
            AwayWinOdds = 4.5
        };

        // Act
        odds.CalculateProbabilities();

        // Assert
        await Assert.That(odds.HomeWinProbability + odds.DrawProbability + odds.AwayWinProbability)
            .IsEqualTo(1d)
            .Within(0.000001);
        await Assert.That(odds.MarketFavorite).IsEqualTo(MatchOutcome.HomeWin);
        await Assert.That(odds.FavoriteConfidence).IsEqualTo(odds.HomeWinProbability).Within(0.000001);
    }

    [Test]
    public async Task CalculateProbabilities_WithInvalidOdds_ShouldSetZeroProbabilities()
    {
        // Arrange
        var odds = new MatchOdds
        {
            MatchId = Guid.NewGuid(),
            HomeWinOdds = 0,
            DrawOdds = 0,
            AwayWinOdds = 0
        };

        // Act
        odds.CalculateProbabilities();

        // Assert
        await Assert.That(odds.HomeWinProbability).IsEqualTo(0d);
        await Assert.That(odds.DrawProbability).IsEqualTo(0d);
        await Assert.That(odds.AwayWinProbability).IsEqualTo(0d);
        await Assert.That(odds.MarketFavorite).IsEqualTo(MatchOutcome.Draw);
        await Assert.That(odds.FavoriteConfidence).IsEqualTo(0d);
    }
}
