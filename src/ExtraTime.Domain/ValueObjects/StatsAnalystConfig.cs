using System.Text.Json;
using ExtraTime.Domain.Enums;

namespace ExtraTime.Domain.ValueObjects;

public sealed record StatsAnalystConfig
{
    public double FormWeight { get; init; } = 0.20;
    public double HomeAdvantageWeight { get; init; } = 0.15;
    public double GoalTrendWeight { get; init; } = 0.10;
    public double StreakWeight { get; init; } = 0.05;
    public double LineupAnalysisWeight { get; init; } = 0.10;
    public double XgWeight { get; init; } = 0.20;
    public double XgDefensiveWeight { get; init; } = 0.10;
    public double OddsWeight { get; init; } = 0.05;
    public double InjuryWeight { get; init; } = 0.05;
    public double EloWeight { get; init; } = 0.00;

    public int MatchesAnalyzed { get; init; } = 5;
    public bool HighStakesBoost { get; init; } = true;
    public int LateSeasonMatchday { get; init; } = 30;

    public PredictionStyle Style { get; init; } = PredictionStyle.Moderate;
    public double RandomVariance { get; init; } = 0.1;
    public bool UseXgData { get; init; } = true;
    public bool UseOddsData { get; init; } = true;
    public bool UseInjuryData { get; init; } = true;
    public bool UseLineupData { get; init; } = true;
    public bool UseEloData { get; init; } = true;

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

    public static StatsAnalystConfig FullAnalysis => new()
    {
        FormWeight = 0.15,
        HomeAdvantageWeight = 0.10,
        GoalTrendWeight = 0.05,
        StreakWeight = 0.05,
        LineupAnalysisWeight = 0.10,
        XgWeight = 0.25,
        XgDefensiveWeight = 0.15,
        OddsWeight = 0.10,
        InjuryWeight = 0.05,
        EloWeight = 0.10
    };

    public static StatsAnalystConfig XgFocused => new()
    {
        FormWeight = 0.10,
        HomeAdvantageWeight = 0.10,
        XgWeight = 0.40,
        XgDefensiveWeight = 0.25,
        OddsWeight = 0.10,
        InjuryWeight = 0.05,
        LineupAnalysisWeight = 0.00,
        EloWeight = 0.00
    };

    public static StatsAnalystConfig MarketFollower => new()
    {
        FormWeight = 0.15,
        HomeAdvantageWeight = 0.10,
        XgWeight = 0.15,
        OddsWeight = 0.50,
        InjuryWeight = 0.10,
        LineupAnalysisWeight = 0.00,
        EloWeight = 0.00,
        Style = PredictionStyle.Conservative
    };

    public static StatsAnalystConfig InjuryAware => new()
    {
        FormWeight = 0.20,
        HomeAdvantageWeight = 0.15,
        XgWeight = 0.20,
        LineupAnalysisWeight = 0.20,
        InjuryWeight = 0.25,
        EloWeight = 0.00
    };
}
