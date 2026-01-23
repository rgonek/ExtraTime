using ExtraTime.Domain.Enums;

namespace ExtraTime.Application.Features.Football.DTOs;

// DTOs from Football-Data.org API
public sealed record MatchApiDto(
    int Id,
    MatchCompetitionApiDto Competition,
    MatchTeamApiDto HomeTeam,
    MatchTeamApiDto AwayTeam,
    DateTime UtcDate,
    string Status,
    int? Matchday,
    string? Stage,
    string? Group,
    ScoreApiDto Score,
    string? Venue);

public sealed record MatchCompetitionApiDto(int Id, string Name);

public sealed record MatchTeamApiDto(int Id, string Name, string ShortName, string? Crest);

public sealed record ScoreApiDto(
    string? Winner,
    string? Duration,
    ScoreDetailApiDto FullTime,
    ScoreDetailApiDto HalfTime);

public sealed record ScoreDetailApiDto(int? Home, int? Away);

public sealed record MatchesApiResponse(IReadOnlyList<MatchApiDto> Matches);

// Application DTOs (for our API responses)
public sealed record MatchDto(
    Guid Id,
    CompetitionSummaryDto Competition,
    TeamSummaryDto HomeTeam,
    TeamSummaryDto AwayTeam,
    DateTime MatchDateUtc,
    MatchStatus Status,
    int? Matchday,
    int? HomeScore,
    int? AwayScore);

public sealed record MatchDetailDto(
    Guid Id,
    CompetitionSummaryDto Competition,
    TeamSummaryDto HomeTeam,
    TeamSummaryDto AwayTeam,
    DateTime MatchDateUtc,
    MatchStatus Status,
    int? Matchday,
    string? Stage,
    string? Group,
    int? HomeScore,
    int? AwayScore,
    int? HomeHalfTimeScore,
    int? AwayHalfTimeScore,
    string? Venue,
    DateTime LastSyncedAt);

public sealed record MatchesPagedResponse(
    IReadOnlyList<MatchDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public sealed record MatchesFilterRequest(
    Guid? CompetitionId = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    MatchStatus? Status = null,
    int Page = 1,
    int PageSize = 20);
