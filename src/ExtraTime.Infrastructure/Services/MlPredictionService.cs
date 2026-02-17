using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.ML.Models;
using ExtraTime.Application.Features.ML.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace ExtraTime.Infrastructure.Services;

public sealed class MlPredictionService(
    IMlFeatureExtractor featureExtractor,
    IApplicationDbContext context,
    ILogger<MlPredictionService> logger) : IMlPredictionService
{
    private const int FeatureVectorSize = 76;
    private readonly MLContext _mlContext = new(seed: 42);
    private readonly SemaphoreSlim _modelLock = new(1, 1);
    private ITransformer? _homeScoreModel;
    private ITransformer? _awayScoreModel;
    private string? _cachedModelVersionKey;

    public async Task<ScorePrediction> PredictScoresAsync(
        Guid matchId,
        CancellationToken cancellationToken = default)
    {
        await EnsureModelsLoadedAsync(cancellationToken);

        var features = await featureExtractor.ExtractFeaturesAsync(matchId, cancellationToken);
        var input = new PredictionInput
        {
            Features = BuildFeatureVector(features)
        };

        var homeEngine = _mlContext.Model.CreatePredictionEngine<PredictionInput, RegressionPrediction>(_homeScoreModel!);
        var awayEngine = _mlContext.Model.CreatePredictionEngine<PredictionInput, RegressionPrediction>(_awayScoreModel!);

        var homePrediction = homeEngine.Predict(input);
        var awayPrediction = awayEngine.Predict(input);

        return new ScorePrediction
        {
            ScoreHome = homePrediction.Score,
            ScoreAway = awayPrediction.Score
        };
    }

    public async Task<List<ScorePrediction>> PredictScoresBatchAsync(
        List<Guid> matchIds,
        CancellationToken cancellationToken = default)
    {
        var predictions = new List<ScorePrediction>(matchIds.Count);

        foreach (var matchId in matchIds)
        {
            predictions.Add(await PredictScoresAsync(matchId, cancellationToken));
        }

        return predictions;
    }

    public async Task<string?> GetActiveModelVersionAsync(
        string modelType = "HomeScore",
        CancellationToken cancellationToken = default)
    {
        return await context.MlModelVersions
            .AsNoTracking()
            .Where(model => model.ModelType == modelType && model.IsActive)
            .Select(model => model.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task EnsureModelsLoadedAsync(CancellationToken cancellationToken)
    {
        var homeVersion = await context.MlModelVersions
            .AsNoTracking()
            .Where(model => model.ModelType == "HomeScore" && model.IsActive)
            .Select(model => new { model.Version, model.BlobPath })
            .FirstOrDefaultAsync(cancellationToken);
        var awayVersion = await context.MlModelVersions
            .AsNoTracking()
            .Where(model => model.ModelType == "AwayScore" && model.IsActive)
            .Select(model => new { model.Version, model.BlobPath })
            .FirstOrDefaultAsync(cancellationToken);

        if (homeVersion is null || awayVersion is null ||
            string.IsNullOrWhiteSpace(homeVersion.BlobPath) ||
            string.IsNullOrWhiteSpace(awayVersion.BlobPath))
        {
            throw new InvalidOperationException("Active ML models were not found.");
        }

        var currentVersionKey = $"{homeVersion.Version}|{awayVersion.Version}";

        if (_homeScoreModel is not null &&
            _awayScoreModel is not null &&
            string.Equals(_cachedModelVersionKey, currentVersionKey, StringComparison.Ordinal))
        {
            return;
        }

        await _modelLock.WaitAsync(cancellationToken);
        try
        {
            if (_homeScoreModel is not null &&
                _awayScoreModel is not null &&
                string.Equals(_cachedModelVersionKey, currentVersionKey, StringComparison.Ordinal))
            {
                return;
            }

            _homeScoreModel = LoadModel(homeVersion.BlobPath);
            _awayScoreModel = LoadModel(awayVersion.BlobPath);
            _cachedModelVersionKey = currentVersionKey;

            logger.LogInformation("Loaded ML models. Home={HomeVersion}, Away={AwayVersion}", homeVersion.Version, awayVersion.Version);
        }
        finally
        {
            _modelLock.Release();
        }
    }

    private ITransformer LoadModel(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("ML model file does not exist.", path);
        }

        using var stream = File.OpenRead(path);
        return _mlContext.Model.Load(stream, out _);
    }

    private static float[] BuildFeatureVector(MatchFeatures features)
    {
        var vector = typeof(MatchFeatures)
            .GetProperties()
            .Where(property => property.PropertyType == typeof(float))
            .Select(property => (float)(property.GetValue(features) ?? 0f))
            .ToArray();

        if (vector.Length != FeatureVectorSize)
        {
            throw new InvalidOperationException(
                $"ML feature vector length mismatch. Expected {FeatureVectorSize} but got {vector.Length}.");
        }

        return vector;
    }

    private sealed class PredictionInput
    {
        [VectorType(FeatureVectorSize)]
        public required float[] Features { get; init; }
    }

    private sealed class RegressionPrediction
    {
        public float Score { get; init; }
    }
}
