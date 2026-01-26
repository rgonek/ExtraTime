using ExtraTime.Domain.Entities;
using ExtraTime.Infrastructure.Services;
using ExtraTime.UnitTests.TestData;

namespace ExtraTime.UnitTests.Infrastructure.Services;

public sealed class BetCalculatorTests
{
    private readonly BetCalculator _calculator = new();
    private readonly League _league = new LeagueBuilder().WithScoringRules(3, 1).Build();

    [Test]
    public async Task CalculateResult_ExactMatch_ReturnsFullPoints()
    {
        // Arrange
        var match = new MatchBuilder().WithScore(2, 1).Build();
        var bet = new BetBuilder().WithPrediction(2, 1).Build();

        // Act
        var result = _calculator.CalculateResult(bet, match, _league);

        // Assert
        await Assert.That(result.PointsEarned).IsEqualTo(3);
        await Assert.That(result.IsExactMatch).IsTrue();
        await Assert.That(result.IsCorrectResult).IsTrue();
    }

    [Test]
    public async Task CalculateResult_CorrectResult_ReturnsPartialPoints()
    {
        // Arrange
        var match = new MatchBuilder().WithScore(2, 1).Build();
        var bet = new BetBuilder().WithPrediction(1, 0).Build();

        // Act
        var result = _calculator.CalculateResult(bet, match, _league);

        // Assert
        await Assert.That(result.PointsEarned).IsEqualTo(1);
        await Assert.That(result.IsExactMatch).IsFalse();
        await Assert.That(result.IsCorrectResult).IsTrue();
    }

    [Test]
    public async Task CalculateResult_WrongResult_ReturnsZeroPoints()
    {
        // Arrange
        var match = new MatchBuilder().WithScore(2, 1).Build();
        var bet = new BetBuilder().WithPrediction(0, 1).Build();

        // Act
        var result = _calculator.CalculateResult(bet, match, _league);

        // Assert
        await Assert.That(result.PointsEarned).IsEqualTo(0);
        await Assert.That(result.IsExactMatch).IsFalse();
        await Assert.That(result.IsCorrectResult).IsFalse();
    }

    [Test]
    public async Task CalculateResult_MatchNotFinished_ReturnsZeroPoints()
    {
        // Arrange
        var match = new MatchBuilder().Build(); // No score
        var bet = new BetBuilder().WithPrediction(2, 1).Build();

        // Act
        var result = _calculator.CalculateResult(bet, match, _league);

        // Assert
        await Assert.That(result.PointsEarned).IsEqualTo(0);
        await Assert.That(result.IsExactMatch).IsFalse();
        await Assert.That(result.IsCorrectResult).IsFalse();
    }

    [Test]
    public async Task CalculateResult_CustomScoring_ReturnsCustomPoints()
    {
        // Arrange
        var customLeague = new LeagueBuilder().WithScoringRules(5, 2).Build();
        var match = new MatchBuilder().WithScore(1, 1).Build();
        var exactBet = new BetBuilder().WithPrediction(1, 1).Build();
        var correctBet = new BetBuilder().WithPrediction(0, 0).Build();

        // Act
        var exactResult = _calculator.CalculateResult(exactBet, match, customLeague);
        var correctResult = _calculator.CalculateResult(correctBet, match, customLeague);

        // Assert
        await Assert.That(exactResult.PointsEarned).IsEqualTo(5);
        await Assert.That(correctResult.PointsEarned).IsEqualTo(2);
    }
}
