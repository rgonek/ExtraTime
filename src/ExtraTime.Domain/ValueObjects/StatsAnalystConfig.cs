using System.Text.Json;
using ExtraTime.Domain.Enums;

namespace ExtraTime.Domain.ValueObjects;

public sealed record StatsAnalystConfig
{
    public double FormWeight { get; init; } = 0.35;
    public double HomeAdvantageWeight { get; init; } = 0.25;
    public double GoalTrendWeight { get; init; } = 0.25;
    public double StreakWeight { get; init; } = 0.15;

    public int MatchesAnalyzed { get; init; } = 5;
    public bool HighStakesBoost { get; init; } = true;
    public int LateSeasonMatchday { get; init; } = 30;

    public PredictionStyle Style { get; init; } = PredictionStyle.Moderate;
    public double RandomVariance { get; init; } = 0.1;

    public int MinGoals => Style switch
    {
        PredictionStyle.Conservative => 0,
        PredictionStyle.Moderate => 0,
        PredictionStyle.Bold => 1,
        _ => 0
    };

    public int MaxGoals => Style switch
    {
        PredictionStyle.Conservative => 2,
        PredictionStyle.Moderate => 4,
        PredictionStyle.Bold => 5,
        _ => 4
    };

    public string ToJson() => JsonSerializer.Serialize(this);

    public static StatsAnalystConfig FromJson(string? json)
    {
        if (string.IsNullOrEmpty(json)) return new StatsAnalystConfig();
        try
        {
            return JsonSerializer.Deserialize<StatsAnalystConfig>(json) ?? new StatsAnalystConfig();
        }
        catch
        {
            return new StatsAnalystConfig();
        }
    }

    public static StatsAnalystConfig Balanced => new();

    public static StatsAnalystConfig FormFocused => new()
    {
        FormWeight = 0.60,
        HomeAdvantageWeight = 0.15,
        GoalTrendWeight = 0.15,
        StreakWeight = 0.10,
        MatchesAnalyzed = 5
    };

    public static StatsAnalystConfig HomeAdvantage => new()
    {
        FormWeight = 0.20,
        HomeAdvantageWeight = 0.50,
        GoalTrendWeight = 0.20,
        StreakWeight = 0.10
    };

    public static StatsAnalystConfig GoalFocused => new()
    {
        FormWeight = 0.25,
        HomeAdvantageWeight = 0.15,
        GoalTrendWeight = 0.50,
        StreakWeight = 0.10,
        Style = PredictionStyle.Bold
    };

    public static StatsAnalystConfig Conservative => new()
    {
        Style = PredictionStyle.Conservative,
        RandomVariance = 0.05
    };

    public static StatsAnalystConfig Chaotic => new()
    {
        RandomVariance = 0.30,
        Style = PredictionStyle.Bold
    };
}
