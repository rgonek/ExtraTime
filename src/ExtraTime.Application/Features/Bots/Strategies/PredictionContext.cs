using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.ValueObjects;

namespace ExtraTime.Application.Features.Bots.Strategies;

/// <summary>
/// Context for a bot prediction, including data availability and retrieved source data.
/// </summary>
public sealed class PredictionContext
{
    public required Match Match { get; init; }
    public required StatsAnalystConfig Config { get; init; }
    public required DataAvailability DataAvailability { get; init; }

    public TeamFormCache? HomeForm { get; set; }
    public TeamFormCache? AwayForm { get; set; }
    public TeamXgStats? HomeXg { get; set; }
    public TeamXgStats? AwayXg { get; set; }
    public MatchOdds? Odds { get; set; }
    public TeamInjuries? HomeInjuries { get; set; }
    public TeamInjuries? AwayInjuries { get; set; }
    public TeamSuspensions? HomeSuspensions { get; set; }
    public TeamSuspensions? AwaySuspensions { get; set; }
    public WeatherContextData? WeatherContext { get; set; }
    public RefereeProfileData? RefereeProfile { get; set; }
    public bool HasLineupAnalysis { get; set; }
    public TeamEloRating? HomeElo { get; set; }
    public TeamEloRating? AwayElo { get; set; }

    public bool CanUseForm => HomeForm is not null && AwayForm is not null;
    public bool CanUseXg => Config.UseXgData && DataAvailability.XgDataAvailable && HomeXg is not null && AwayXg is not null;
    public bool CanUseOdds => Config.UseOddsData && DataAvailability.OddsDataAvailable && Odds is not null;
    public bool CanUseInjuries =>
        Config.UseInjuryData &&
        DataAvailability.InjuryDataAvailable &&
        (HomeInjuries is not null || AwayInjuries is not null);
    public bool HasSuspensionContext =>
        DataAvailability.SuspensionDataAvailable &&
        (HomeSuspensions is not null || AwaySuspensions is not null);
    public bool HasWeatherContext =>
        DataAvailability.WeatherDataAvailable &&
        WeatherContext is not null;
    public bool HasRefereeContext =>
        DataAvailability.RefereeDataAvailable &&
        RefereeProfile is not null;
    public bool CanUseLineups => Config.UseLineupData && DataAvailability.LineupDataAvailable && HasLineupAnalysis;
    public bool CanUseElo =>
        Config.UseEloData &&
        DataAvailability.EloDataAvailable &&
        HomeElo is not null &&
        AwayElo is not null;

    public EffectiveWeights CalculateEffectiveWeights()
    {
        var configured = new List<(double Weight, bool IsAvailable)>
        {
            (Config.FormWeight, CanUseForm),
            (Config.HomeAdvantageWeight, true),
            (Config.XgWeight, CanUseXg),
            (Config.XgDefensiveWeight, CanUseXg),
            (Config.OddsWeight, CanUseOdds),
            (Config.InjuryWeight, CanUseInjuries),
            (Config.LineupAnalysisWeight, CanUseLineups),
            (Config.EloWeight, CanUseElo)
        };

        var totalConfiguredWeight = configured
            .Where(x => x.Weight > 0)
            .Sum(x => x.Weight);
        var totalAvailableWeight = configured
            .Where(x => x.Weight > 0 && x.IsAvailable)
            .Sum(x => x.Weight);
        var scaleFactor = totalAvailableWeight > 0 && totalConfiguredWeight > 0
            ? totalConfiguredWeight / totalAvailableWeight
            : 1.0;
        var dataQualityScore = totalConfiguredWeight > 0
            ? (totalAvailableWeight / totalConfiguredWeight) * 100
            : 100.0;

        return new EffectiveWeights
        {
            FormWeight = CanUseForm ? Config.FormWeight * scaleFactor : 0,
            HomeAdvantageWeight = Config.HomeAdvantageWeight > 0
                ? Config.HomeAdvantageWeight * scaleFactor
                : 0,
            XgWeight = CanUseXg ? Config.XgWeight * scaleFactor : 0,
            XgDefensiveWeight = CanUseXg ? Config.XgDefensiveWeight * scaleFactor : 0,
            OddsWeight = CanUseOdds ? Config.OddsWeight * scaleFactor : 0,
            InjuryWeight = CanUseInjuries ? Config.InjuryWeight * scaleFactor : 0,
            LineupWeight = CanUseLineups ? Config.LineupAnalysisWeight * scaleFactor : 0,
            EloWeight = CanUseElo ? Config.EloWeight * scaleFactor : 0,
            TotalConfiguredSources = configured.Count(x => x.Weight > 0),
            TotalAvailableSources = configured.Count(x => x.Weight > 0 && x.IsAvailable),
            DataQualityScore = dataQualityScore
        };
    }

    public bool CanMakePrediction()
    {
        if (!CanUseForm)
        {
            return false;
        }

        var effectiveWeights = CalculateEffectiveWeights();
        return effectiveWeights.DataQualityScore >= 50;
    }

    public string? GetDegradationWarning()
    {
        var missing = new List<string>();

        if (Config.XgWeight > 0.15 && !CanUseXg)
        {
            missing.Add("xG data unavailable");
        }

        if (Config.OddsWeight > 0.15 && !CanUseOdds)
        {
            missing.Add("odds data unavailable");
        }

        if (Config.InjuryWeight > 0.10 && !CanUseInjuries)
        {
            missing.Add("injury data unavailable");
        }

        if (Config.LineupAnalysisWeight > 0.10 && !CanUseLineups)
        {
            missing.Add("lineup data unavailable");
        }

        if (Config.EloWeight > 0.10 && !CanUseElo)
        {
            missing.Add("Elo rating data unavailable");
        }

        return missing.Count > 0
            ? $"Degraded prediction: {string.Join(", ", missing)}"
            : null;
    }
}

public sealed record EffectiveWeights
{
    public double FormWeight { get; init; }
    public double HomeAdvantageWeight { get; init; }
    public double XgWeight { get; init; }
    public double XgDefensiveWeight { get; init; }
    public double OddsWeight { get; init; }
    public double InjuryWeight { get; init; }
    public double LineupWeight { get; init; }
    public double EloWeight { get; init; }
    public int TotalConfiguredSources { get; init; }
    public int TotalAvailableSources { get; init; }
    public double DataQualityScore { get; init; }
}
