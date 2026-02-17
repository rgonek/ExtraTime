namespace ExtraTime.Application.Features.Bots.DTOs;

public sealed record BotDto(
    Guid Id,
    string Name,
    string? AvatarUrl,
    string Strategy,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastBetPlacedAt,
    string? Configuration = null,
    BotStatsDto? Stats = null);

public sealed record BotStatsDto(
    int TotalBetsPlaced,
    int LeaguesJoined,
    double AveragePointsPerBet,
    int ExactPredictions,
    int CorrectResults);

public sealed record BotSummaryDto(
    Guid Id,
    string Name,
    string? AvatarUrl,
    string Strategy);

public sealed record LeagueBotDto(
    Guid Id,
    string Name,
    string? AvatarUrl,
    string Strategy,
    DateTime AddedAt);

public sealed record CreateBotRequest(
    string Name,
    string? AvatarUrl,
    string Strategy,
    Dictionary<string, object>? Configuration = null);

public sealed record UpdateBotRequest(
    string? Name,
    string? AvatarUrl,
    string? Strategy,
    Dictionary<string, object>? Configuration,
    bool? IsActive);

public sealed record AddBotToLeagueRequest(Guid BotId);

public sealed record CreateStatsAnalystBotRequest(
    string Name,
    string? AvatarUrl,
    double FormWeight = 0.35,
    double HomeAdvantageWeight = 0.25,
    double GoalTrendWeight = 0.25,
    double StreakWeight = 0.15,
    int MatchesAnalyzed = 5,
    bool HighStakesBoost = true,
    string Style = "Moderate",
    double RandomVariance = 0.1);

public sealed record BotConfigurationDto(
    double FormWeight,
    double HomeAdvantageWeight,
    double GoalTrendWeight,
    double StreakWeight,
    double XgWeight,
    double XgDefensiveWeight,
    double OddsWeight,
    double InjuryWeight,
    double LineupAnalysisWeight,
    int MatchesAnalyzed,
    bool HighStakesBoost,
    string Style,
    double RandomVariance,
    bool UseXgData,
    bool UseOddsData,
    bool UseInjuryData,
    bool UseLineupData,
    bool UseEloData);

public sealed record ConfigurationPresetDto(
    string Name,
    string Description,
    BotConfigurationDto Configuration);
