using ExtraTime.Application.Features.ML.Models;

namespace ExtraTime.UnitTests.Application.Features.ML.Models;

public sealed class ScorePredictionTests
{
    [Test]
    public async Task PredictedScores_ShouldClampToExpectedRange()
    {
        var prediction = new ScorePrediction
        {
            ScoreHome = 7.2f,
            ScoreAway = -0.8f
        };

        await Assert.That(prediction.PredictedHomeScore).IsEqualTo(5);
        await Assert.That(prediction.PredictedAwayScore).IsEqualTo(0);
    }

    [Test]
    public async Task PredictedOutcome_ShouldMatchRoundedScores()
    {
        var prediction = new ScorePrediction
        {
            ScoreHome = 1.6f,
            ScoreAway = 1.2f
        };

        await Assert.That(prediction.PredictedScore).IsEqualTo("2-1");
        await Assert.That(prediction.PredictedOutcome).IsEqualTo("HomeWin");
    }
}
