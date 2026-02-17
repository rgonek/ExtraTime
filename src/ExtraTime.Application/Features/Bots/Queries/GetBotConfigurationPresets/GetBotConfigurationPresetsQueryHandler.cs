using ExtraTime.Application.Common;
using ExtraTime.Application.Features.Bots.DTOs;
using ExtraTime.Domain.ValueObjects;
using Mediator;

namespace ExtraTime.Application.Features.Bots.Queries.GetBotConfigurationPresets;

public sealed class GetBotConfigurationPresetsQueryHandler : IRequestHandler<GetBotConfigurationPresetsQuery, Result<List<ConfigurationPresetDto>>>
{
    public ValueTask<Result<List<ConfigurationPresetDto>>> Handle(GetBotConfigurationPresetsQuery request, CancellationToken cancellationToken)
    {
        var presets = new List<ConfigurationPresetDto>
        {
            new("Balanced", "All-round analysis using all available data", MapConfig(StatsAnalystConfig.Balanced)),
            new("Form Focused", "Heavily weights recent match results", MapConfig(StatsAnalystConfig.FormFocused)),
            new("Home Advantage", "Believes home teams always win", MapConfig(StatsAnalystConfig.HomeAdvantage)),
            new("Goal Focused", "Predicts high-scoring matches", MapConfig(StatsAnalystConfig.GoalFocused)),
            new("Conservative", "Low-risk, low-score predictions", MapConfig(StatsAnalystConfig.Conservative)),
            new("Chaotic", "Unpredictable wild predictions", MapConfig(StatsAnalystConfig.Chaotic)),
            new("Full Analysis", "Uses all external data sources", MapConfig(StatsAnalystConfig.FullAnalysis)),
            new("xG Expert", "Heavy expected goals weighting", MapConfig(StatsAnalystConfig.XgFocused)),
            new("Market Follower", "Follows betting odds consensus", MapConfig(StatsAnalystConfig.MarketFollower)),
            new("Injury Aware", "Focuses on squad availability", MapConfig(StatsAnalystConfig.InjuryAware))
        };

        return ValueTask.FromResult(Result<List<ConfigurationPresetDto>>.Success(presets));
    }

    private static BotConfigurationDto MapConfig(StatsAnalystConfig config)
    {
        return new BotConfigurationDto(
            FormWeight: config.FormWeight,
            HomeAdvantageWeight: config.HomeAdvantageWeight,
            GoalTrendWeight: config.GoalTrendWeight,
            StreakWeight: config.StreakWeight,
            XgWeight: config.XgWeight,
            XgDefensiveWeight: config.XgDefensiveWeight,
            OddsWeight: config.OddsWeight,
            InjuryWeight: config.InjuryWeight,
            LineupAnalysisWeight: config.LineupAnalysisWeight,
            MatchesAnalyzed: config.MatchesAnalyzed,
            HighStakesBoost: config.HighStakesBoost,
            Style: config.Style.ToString(),
            RandomVariance: config.RandomVariance,
            UseXgData: config.UseXgData,
            UseOddsData: config.UseOddsData,
            UseInjuryData: config.UseInjuryData,
            UseLineupData: config.UseLineupData,
            UseEloData: config.UseEloData);
    }
}
