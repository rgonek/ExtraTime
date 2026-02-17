using System.Text.Json;
using ExtraTime.Application.Features.ML.Services;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Application.Features.Bots.Strategies;

public sealed class MachineLearningStrategy(
    IMlPredictionService predictionService,
    ILogger<MachineLearningStrategy>? logger = null,
    FallbackStrategy? fallbackStrategy = null) : IBotBettingStrategy
{
    private readonly FallbackStrategy _fallbackStrategy = fallbackStrategy ?? new FallbackStrategy();

    public BotStrategy StrategyType => BotStrategy.MachineLearning;

    public (int HomeScore, int AwayScore) GeneratePrediction(Match match, string? configuration)
    {
        return _fallbackStrategy.GenerateBasicPrediction();
    }

    public async Task<(int HomeScore, int AwayScore)> GeneratePredictionAsync(
        Match match,
        string? configuration,
        CancellationToken cancellationToken = default)
    {
        var config = MachineLearningConfig.FromJson(configuration);
        var activeVersion = await predictionService.GetActiveModelVersionAsync(
            cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(activeVersion))
        {
            logger?.LogWarning("No active ML model found. Falling back to baseline for match {MatchId}", match.Id);
            return _fallbackStrategy.GenerateBasicPrediction();
        }

        try
        {
            var prediction = await predictionService.PredictScoresAsync(match.Id, cancellationToken);
            var adjusted = ApplyRiskProfile(
                prediction.PredictedHomeScore,
                prediction.PredictedAwayScore,
                config.RiskProfile);

            return adjusted;
        }
        catch (InvalidOperationException ex)
        {
            logger?.LogWarning(ex, "ML prediction unavailable for match {MatchId}. Falling back to baseline.", match.Id);
            return _fallbackStrategy.GenerateBasicPrediction();
        }
        catch (FileNotFoundException ex)
        {
            logger?.LogWarning(ex, "ML model file missing for match {MatchId}. Falling back to baseline.", match.Id);
            return _fallbackStrategy.GenerateBasicPrediction();
        }
    }

    private static (int HomeScore, int AwayScore) ApplyRiskProfile(int homeScore, int awayScore, string riskProfile)
    {
        return riskProfile.ToLowerInvariant() switch
        {
            "conservative" => (Math.Max(0, homeScore - 1), Math.Max(0, awayScore - 1)),
            "aggressive" => (Math.Min(5, homeScore + 1), Math.Min(5, awayScore + 1)),
            _ => (homeScore, awayScore)
        };
    }
}

public sealed record MachineLearningConfig(string RiskProfile = "balanced")
{
    public static MachineLearningConfig FromJson(string? configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration))
        {
            return new MachineLearningConfig();
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<MachineLearningConfig>(
                configuration,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return parsed ?? new MachineLearningConfig();
        }
        catch (JsonException)
        {
            return new MachineLearningConfig();
        }
    }
}
