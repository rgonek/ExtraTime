using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.ML.Models;
using ExtraTime.Application.Features.ML.Services;
using ExtraTime.Domain.Entities;
using ExtraTime.MLTrainer.Models;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.Extensions.Logging;

namespace ExtraTime.MLTrainer.Services;

public sealed class ModelTrainer(
    IMlFeatureExtractor featureExtractor,
    IApplicationDbContext context,
    ILogger<ModelTrainer> logger)
{
    private const int FeatureVectorSize = 76;
    private readonly MLContext _mlContext = new(seed: 42);

    public async Task<TrainingResult> TrainAsync(
        string league,
        DateTime? fromDate = null,
        CancellationToken cancellationToken = default)
    {
        var trainingData = await featureExtractor.GetTrainingDataAsync(
            fromDate: fromDate,
            toDate: DateTime.UtcNow.AddDays(-1),
            league: league,
            cancellationToken: cancellationToken);

        if (trainingData.Count < 20)
        {
            throw new InvalidOperationException(
                $"Insufficient training samples for ML training: {trainingData.Count}.");
        }

        var rows = trainingData.Select(sample => new TrainingRow
        {
            Features = BuildFeatureVector(sample.Features),
            HomeScore = sample.ActualHomeScore,
            AwayScore = sample.ActualAwayScore
        }).ToList();

        var dataView = _mlContext.Data.LoadFromEnumerable(rows);
        var split = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

        var homeModelResult = TrainSingleTarget(split.TrainSet, split.TestSet, nameof(TrainingRow.HomeScore));
        var awayModelResult = TrainSingleTarget(split.TrainSet, split.TestSet, nameof(TrainingRow.AwayScore));

        var version = $"v{DateTime.UtcNow:yyyyMMddHHmmss}";
        var homeModelPath = await SaveModelAsync(
            league,
            "HomeScore",
            version,
            homeModelResult.Model,
            homeModelResult.Schema,
            cancellationToken);
        var awayModelPath = await SaveModelAsync(
            league,
            "AwayScore",
            version,
            awayModelResult.Model,
            awayModelResult.Schema,
            cancellationToken);

        await SaveMetadataAsync("HomeScore", version, homeModelPath, rows.Count, homeModelResult.Metrics, cancellationToken);
        await SaveMetadataAsync("AwayScore", version, awayModelPath, rows.Count, awayModelResult.Metrics, cancellationToken);

        return new TrainingResult(
            version,
            rows.Count,
            new ModelMetricsResult(
                homeModelResult.Metrics.RSquared,
                homeModelResult.Metrics.MeanAbsoluteError,
                homeModelResult.Metrics.RootMeanSquaredError),
            new ModelMetricsResult(
                awayModelResult.Metrics.RSquared,
                awayModelResult.Metrics.MeanAbsoluteError,
                awayModelResult.Metrics.RootMeanSquaredError));
    }

    public static float[] BuildFeatureVector(MatchFeatures features)
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

    private (ITransformer Model, RegressionMetrics Metrics, DataViewSchema Schema) TrainSingleTarget(
        IDataView trainData,
        IDataView testData,
        string labelColumnName)
    {
        var pipeline = _mlContext.Transforms.NormalizeMinMax(nameof(TrainingRow.Features))
            .Append(_mlContext.Regression.Trainers.FastTree(
                labelColumnName: labelColumnName,
                featureColumnName: nameof(TrainingRow.Features),
                numberOfLeaves: 32,
                numberOfTrees: 200,
                minimumExampleCountPerLeaf: 8));

        var model = pipeline.Fit(trainData);
        var predictions = model.Transform(testData);
        var metrics = _mlContext.Regression.Evaluate(predictions, labelColumnName: labelColumnName);

        logger.LogInformation(
            "{Label} model metrics: R2={R2:F3}, MAE={Mae:F3}, RMSE={Rmse:F3}",
            labelColumnName,
            metrics.RSquared,
            metrics.MeanAbsoluteError,
            metrics.RootMeanSquaredError);

        return (model, metrics, trainData.Schema);
    }

    private static async Task<string> SaveModelAsync(
        string league,
        string modelType,
        string version,
        ITransformer model,
        DataViewSchema schema,
        CancellationToken cancellationToken)
    {
        var modelDirectory = Path.Combine("artifacts", "ml-models", league, modelType);
        Directory.CreateDirectory(modelDirectory);

        var modelPath = Path.Combine(modelDirectory, $"{version}.zip");
        using var fileStream = File.Create(modelPath);
        var mlContext = new MLContext(seed: 42);
        mlContext.Model.Save(model, schema, fileStream);
        await fileStream.FlushAsync(cancellationToken);

        return modelPath;
    }

    private async Task SaveMetadataAsync(
        string modelType,
        string version,
        string modelPath,
        int trainingSamples,
        RegressionMetrics metrics,
        CancellationToken cancellationToken)
    {
        var modelVersion = new MlModelVersion
        {
            ModelType = modelType,
            Version = version,
            BlobPath = modelPath,
            TrainedAt = DateTime.UtcNow,
            TrainingSamples = trainingSamples,
            Rsquared = metrics.RSquared,
            MeanAbsoluteError = metrics.MeanAbsoluteError,
            RootMeanSquaredError = metrics.RootMeanSquaredError,
            IsActive = false,
            AlgorithmUsed = "FastTree"
        };

        context.MlModelVersions.Add(modelVersion);
        await context.SaveChangesAsync(cancellationToken);
    }

    private sealed class TrainingRow
    {
        [VectorType(FeatureVectorSize)]
        public required float[] Features { get; init; }

        public float HomeScore { get; init; }
        public float AwayScore { get; init; }
    }
}
