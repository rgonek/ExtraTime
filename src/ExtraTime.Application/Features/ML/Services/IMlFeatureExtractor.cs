using ExtraTime.Application.Features.ML.Models;

namespace ExtraTime.Application.Features.ML.Services;

public interface IMlFeatureExtractor
{
    Task<MatchFeatures> ExtractFeaturesAsync(
        Guid matchId,
        CancellationToken cancellationToken = default);

    Task<List<MatchFeatures>> ExtractFeaturesBatchAsync(
        List<Guid> matchIds,
        CancellationToken cancellationToken = default);

    Task<List<(MatchFeatures Features, int ActualHomeScore, int ActualAwayScore)>> GetTrainingDataAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? league = null,
        CancellationToken cancellationToken = default);
}
