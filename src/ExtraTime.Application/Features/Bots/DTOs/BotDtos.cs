namespace ExtraTime.Application.Features.Bots.DTOs;

public sealed record BotDto(
    Guid Id,
    string Name,
    string? AvatarUrl,
    string Strategy,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastBetPlacedAt);

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
    string Strategy);

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
