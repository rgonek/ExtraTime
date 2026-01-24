using ExtraTime.Domain.Enums;

namespace ExtraTime.Application.Features.Bets.DTOs;

// Request DTOs
public sealed record PlaceBetRequest(
    Guid MatchId,
    int PredictedHomeScore,
    int PredictedAwayScore);

// Response DTOs
public sealed record BetDto(
    Guid Id,
    Guid LeagueId,
    Guid UserId,
    Guid MatchId,
    int PredictedHomeScore,
    int PredictedAwayScore,
    DateTime PlacedAt,
    DateTime? LastUpdatedAt,
    BetResultDto? Result);

public sealed record BetResultDto(
    int PointsEarned,
    bool IsExactMatch,
    bool IsCorrectResult);

public sealed record MyBetDto(
    Guid BetId,
    Guid MatchId,
    string HomeTeamName,
    string AwayTeamName,
    DateTime MatchDateUtc,
    MatchStatus MatchStatus,
    int? ActualHomeScore,
    int? ActualAwayScore,
    int PredictedHomeScore,
    int PredictedAwayScore,
    BetResultDto? Result,
    DateTime PlacedAt);

public sealed record MatchBetDto(
    Guid UserId,
    string Username,
    int PredictedHomeScore,
    int PredictedAwayScore,
    BetResultDto? Result);

public sealed record LeagueStandingDto(
    Guid UserId,
    string Username,
    string Email,
    int Rank,
    int TotalPoints,
    int BetsPlaced,
    int ExactMatches,
    int CorrectResults,
    int CurrentStreak,
    int BestStreak,
    DateTime LastUpdatedAt);

public sealed record UserStatsDto(
    Guid UserId,
    string Username,
    int TotalPoints,
    int BetsPlaced,
    int ExactMatches,
    int CorrectResults,
    int CurrentStreak,
    int BestStreak,
    double AccuracyPercentage,
    int Rank,
    DateTime LastUpdatedAt);
