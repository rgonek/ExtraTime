using ExtraTime.Application.Features.Bots.Strategies;
using ExtraTime.Application.Features.ML.Models;
using ExtraTime.Application.Features.ML.Services;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ExtraTime.UnitTests.Application.Services.BotStrategies;

public sealed class MachineLearningStrategyTests
{
    [Test]
    public async Task GeneratePredictionAsync_WithActiveModel_ReturnsRoundedPrediction()
    {
        var predictionService = Substitute.For<IMlPredictionService>();
        predictionService.GetActiveModelVersionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("v-test");
        predictionService.PredictScoresAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ScorePrediction
            {
                ScoreHome = 1.6f,
                ScoreAway = 0.9f
            });

        var strategy = new MachineLearningStrategy(
            predictionService,
            Substitute.For<ILogger<MachineLearningStrategy>>());
        var match = Match.Create(123, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, MatchStatus.Scheduled);

        var (home, away) = await strategy.GeneratePredictionAsync(match, null);

        await Assert.That(home).IsEqualTo(2);
        await Assert.That(away).IsEqualTo(1);
    }

    [Test]
    public async Task GeneratePredictionAsync_WithAggressiveConfig_AdjustsScoresUp()
    {
        var predictionService = Substitute.For<IMlPredictionService>();
        predictionService.GetActiveModelVersionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("v-test");
        predictionService.PredictScoresAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ScorePrediction
            {
                ScoreHome = 2.1f,
                ScoreAway = 1.1f
            });

        var strategy = new MachineLearningStrategy(
            predictionService,
            Substitute.For<ILogger<MachineLearningStrategy>>());
        var match = Match.Create(123, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, MatchStatus.Scheduled);

        var (home, away) = await strategy.GeneratePredictionAsync(
            match,
            "{\"riskProfile\":\"aggressive\"}");

        await Assert.That(home).IsEqualTo(3);
        await Assert.That(away).IsEqualTo(2);
    }

    [Test]
    public async Task StrategyType_ShouldBeMachineLearning()
    {
        var strategy = new MachineLearningStrategy(
            Substitute.For<IMlPredictionService>(),
            Substitute.For<ILogger<MachineLearningStrategy>>());

        await Assert.That(strategy.StrategyType).IsEqualTo(BotStrategy.MachineLearning);
    }
}
