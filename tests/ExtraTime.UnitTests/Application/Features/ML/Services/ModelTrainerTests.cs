using ExtraTime.Application.Features.ML.Models;
using ExtraTime.MLTrainer.Services;

namespace ExtraTime.UnitTests.Application.Features.ML.Services;

public sealed class ModelTrainerTests
{
    [Test]
    public async Task BuildFeatureVector_ShouldIncludeOnlyFloatProperties()
    {
        var features = new MatchFeatures
        {
            MatchId = Guid.NewGuid().ToString(),
            HomeTeamId = Guid.NewGuid().ToString(),
            AwayTeamId = Guid.NewGuid().ToString(),
            HomeFormPointsLast5 = 11f,
            AwayFormPointsLast5 = 8f,
            HomeOdds = 1.9f,
            AwayOdds = 4.2f
        };

        var vector = ModelTrainer.BuildFeatureVector(features);

        await Assert.That(vector.Length).IsGreaterThan(20);
        await Assert.That(vector.Contains(11f)).IsTrue();
        await Assert.That(vector.Contains(8f)).IsTrue();
        await Assert.That(vector.Contains(1.9f)).IsTrue();
        await Assert.That(vector.Contains(4.2f)).IsTrue();
    }
}
