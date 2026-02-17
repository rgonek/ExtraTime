using ExtraTime.Application.Features.ML.Models;

namespace ExtraTime.Application.Features.ML.Services;

public interface IMlPredictionService
{
    Task<ScorePrediction> PredictScoresAsync(
        Guid matchId,
        CancellationToken cancellationToken = default);

    Task<List<ScorePrediction>> PredictScoresBatchAsync(
        List<Guid> matchIds,
        CancellationToken cancellationToken = default);

    Task<string?> GetActiveModelVersionAsync(
        string modelType = "HomeScore",
        CancellationToken cancellationToken = default);
}
