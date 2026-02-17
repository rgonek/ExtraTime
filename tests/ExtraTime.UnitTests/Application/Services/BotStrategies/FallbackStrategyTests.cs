using ExtraTime.Application.Features.Bots.Strategies;

namespace ExtraTime.UnitTests.Application.Services.BotStrategies;

public sealed class FallbackStrategyTests
{
    [Test]
    public async Task GenerateBasicPrediction_ReturnsExpectedScoreRange()
    {
        // Arrange
        var strategy = new FallbackStrategy(new Random(42));

        // Act
        var (home, away) = strategy.GenerateBasicPrediction();

        // Assert
        await Assert.That(home).IsGreaterThanOrEqualTo(1);
        await Assert.That(home).IsLessThanOrEqualTo(2);
        await Assert.That(away).IsGreaterThanOrEqualTo(0);
        await Assert.That(away).IsLessThanOrEqualTo(2);
    }
}
