using ExtraTime.Application.Features.ML.Models;
using ExtraTime.Application.Features.ML.Services;
using ExtraTime.Domain.Entities;
using ExtraTime.Infrastructure.Data;
using ExtraTime.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;
using NSubstitute;

namespace ExtraTime.UnitTests.Infrastructure.Services;

public sealed class MlPredictionServiceTests
{
    private const int FeatureVectorSize = 76;

    [Test]
    public async Task PredictScoresAsync_WithActiveModels_ReturnsPrediction()
    {
        await using var context = CreateContext();
        var modelDirectory = Path.Combine(Path.GetTempPath(), $"ml-models-{Guid.NewGuid():N}");
        Directory.CreateDirectory(modelDirectory);

        try
        {
            var homePath = Path.Combine(modelDirectory, "home.zip");
            var awayPath = Path.Combine(modelDirectory, "away.zip");

            TrainModel(homePath, row => row.Features[0] * 0.3f + 1.2f);
            TrainModel(awayPath, row => row.Features[1] * 0.2f + 0.8f);

            context.MlModelVersions.AddRange(
                CreateModelVersion("HomeScore", "v-test", homePath, true),
                CreateModelVersion("AwayScore", "v-test", awayPath, true));
            await context.SaveChangesAsync();

            var matchId = Guid.NewGuid();
            var featureExtractor = Substitute.For<IMlFeatureExtractor>();
            featureExtractor.ExtractFeaturesAsync(matchId, Arg.Any<CancellationToken>())
                .Returns(CreateFeatures(matchId));

            var service = new MlPredictionService(
                featureExtractor,
                context,
                Substitute.For<ILogger<MlPredictionService>>());

            var prediction = await service.PredictScoresAsync(matchId);

            await Assert.That(prediction.PredictedHomeScore).IsGreaterThanOrEqualTo(0);
            await Assert.That(prediction.PredictedAwayScore).IsGreaterThanOrEqualTo(0);
            await Assert.That(prediction.PredictedHomeScore).IsLessThanOrEqualTo(5);
            await Assert.That(prediction.PredictedAwayScore).IsLessThanOrEqualTo(5);
        }
        finally
        {
            if (Directory.Exists(modelDirectory))
            {
                Directory.Delete(modelDirectory, recursive: true);
            }
        }
    }

    [Test]
    public async Task GetActiveModelVersionAsync_ReturnsConfiguredVersion()
    {
        await using var context = CreateContext();
        context.MlModelVersions.Add(CreateModelVersion("HomeScore", "v-active", "C:\\temp\\home.zip", true));
        await context.SaveChangesAsync();

        var service = new MlPredictionService(
            Substitute.For<IMlFeatureExtractor>(),
            context,
            Substitute.For<ILogger<MlPredictionService>>());

        var version = await service.GetActiveModelVersionAsync("HomeScore");

        await Assert.That(version).IsEqualTo("v-active");
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static MlModelVersion CreateModelVersion(string modelType, string version, string path, bool isActive)
    {
        return new MlModelVersion
        {
            ModelType = modelType,
            Version = version,
            BlobPath = path,
            IsActive = isActive,
            TrainedAt = DateTime.UtcNow,
            TrainingSamples = 100,
            Rsquared = 0.3,
            MeanAbsoluteError = 0.9,
            RootMeanSquaredError = 1.2
        };
    }

    private static MatchFeatures CreateFeatures(Guid matchId)
    {
        return new MatchFeatures
        {
            MatchId = matchId.ToString(),
            HomeTeamId = Guid.NewGuid().ToString(),
            AwayTeamId = Guid.NewGuid().ToString(),
            HomeFormPointsLast5 = 12f,
            AwayFormPointsLast5 = 8f,
            HomeOdds = 1.9f,
            AwayOdds = 3.8f
        };
    }

    private static void TrainModel(string outputPath, Func<TrainingRow, float> labelFactory)
    {
        var mlContext = new MLContext(seed: 7);
        var rows = Enumerable.Range(1, 60)
            .Select(i =>
            {
                var row = new TrainingRow
                {
                    Features = CreateTrainingVector(i)
                };
                row.Score = labelFactory(row);
                return row;
            })
            .ToList();

        var data = mlContext.Data.LoadFromEnumerable(rows);
        var pipeline = mlContext.Transforms.NormalizeMinMax(nameof(TrainingRow.Features))
            .Append(mlContext.Regression.Trainers.FastTree(
                labelColumnName: nameof(TrainingRow.Score),
                featureColumnName: nameof(TrainingRow.Features),
                numberOfLeaves: 20,
                numberOfTrees: 80));
        var model = pipeline.Fit(data);

        using var stream = File.Create(outputPath);
        mlContext.Model.Save(model, data.Schema, stream);
    }

    private sealed class TrainingRow
    {
        [VectorType(FeatureVectorSize)]
        public required float[] Features { get; set; }

        public float Score { get; set; }
    }

    private static float[] CreateTrainingVector(int seed)
    {
        var vector = new float[FeatureVectorSize];
        vector[0] = seed % 6;
        vector[1] = seed % 5;
        vector[2] = (seed % 4) + 0.5f;
        vector[3] = (seed % 3) + 0.25f;

        for (var index = 4; index < vector.Length; index++)
        {
            vector[index] = (seed + index) % 7;
        }

        return vector;
    }
}
